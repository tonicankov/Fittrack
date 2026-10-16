using FitTrack.Api.Common;
using FitTrack.Api.Data;
using FitTrack.Api.Domain;
using FitTrack.Api.Features.Feed;
using FitTrack.Api.Features.Friends;
using FitTrack.Api.Features.Meals;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Features.Goals;

public sealed class GoalService(
    AppDbContext db,
    MemberSettingsProvider members,
    VisibilityService visibility,
    FeedService feed,
    TimeProvider time)
{
    public async Task<GoalDto> CreateAsync(Guid userId, CreateGoalRequest request, CancellationToken ct)
    {
        var settings = await members.GetAsync(userId, ct);
        var today = TimeZones.Today(time, settings.TimeZone);

        var metric = request.Metric ?? throw RequestValidationException.For("metric", "Required.");
        var v = new Validator();
        ValidateTarget(v, metric, request.TargetValue, request.TargetDate, today);
        v.ThrowIfInvalid();

        if (await db.Goals.AnyAsync(g => g.UserId == userId && g.Metric == metric && g.Status == GoalStatus.Active, ct))
        {
            throw new ConflictException("You already have an active goal of this kind. Archive it first.");
        }

        var goal = new Goal
        {
            UserId = userId,
            Metric = metric,
            TargetValue = request.TargetValue!.Value,
            StartValue = metric == GoalMetric.BodyWeight ? await LatestWeightAsync(userId, ct) : null,
            StartDate = today,
            TargetDate = metric == GoalMetric.BodyWeight ? request.TargetDate : null,
            Visibility = request.Visibility ?? settings.DefaultVisibility,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };
        db.Goals.Add(goal);
        await db.SaveChangesAsync(ct);

        return ToDto(goal, await LoadProgressInputsAsync(userId, ct));
    }

    public async Task<GoalDto> UpdateAsync(Guid userId, Guid id, UpdateGoalRequest request, CancellationToken ct)
    {
        var goal = await LoadOwnAsync(userId, id, ct);
        var settings = await members.GetAsync(userId, ct);
        var today = TimeZones.Today(time, settings.TimeZone);

        var v = new Validator();
        ValidateTarget(v, goal.Metric, request.TargetValue ?? goal.TargetValue, request.TargetDate, today);
        if (request.Status == GoalStatus.Achieved)
        {
            v.Add("status", "A goal is marked as achieved automatically.");
        }
        v.ThrowIfInvalid();

        if (request.Status == GoalStatus.Active && goal.Status != GoalStatus.Active
            && await db.Goals.AnyAsync(g => g.UserId == userId && g.Metric == goal.Metric && g.Status == GoalStatus.Active, ct))
        {
            throw new ConflictException("You already have an active goal of this kind. Archive it first.");
        }

        goal.TargetValue = request.TargetValue ?? goal.TargetValue;
        if (goal.Metric == GoalMetric.BodyWeight)
        {
            goal.TargetDate = request.TargetDate;
        }
        var wasShared = goal.Visibility == Visibility.Friends;
        goal.Visibility = request.Visibility ?? goal.Visibility;
        if (request.Status is { } status)
        {
            goal.Status = status;
            goal.AchievedAt = status == GoalStatus.Active ? null : goal.AchievedAt;
        }

        await db.SaveChangesAsync(ct);

        if (wasShared && goal.Visibility == Visibility.Private)
        {
            await feed.RemoveForGoalAsync(goal.Id, ct);
        }

        return ToDto(goal, await LoadProgressInputsAsync(userId, ct));
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var goal = await LoadOwnAsync(userId, id, ct);
        await feed.RemoveForGoalAsync(goal.Id, ct);
        db.Goals.Remove(goal);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Active goals first, then the rest, newest first.</summary>
    public async Task<List<GoalDto>> ListAsync(Guid viewerId, Guid ownerId, CancellationToken ct)
    {
        var goals = await visibility.VisibleTo(db.Goals.AsNoTracking(), viewerId)
            .Where(g => g.UserId == ownerId)
            .OrderBy(g => g.Status == GoalStatus.Active ? 0 : 1)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync(ct);

        if (goals.Count == 0)
        {
            return [];
        }

        var inputs = await LoadProgressInputsAsync(ownerId, ct);
        var dtos = goals.Select(g => ToDto(g, inputs));

        // Progress can be computed from private entries (a private weigh-in, private meals).
        // Friends see how far along a shared goal is, but not the underlying numbers.
        return viewerId == ownerId
            ? dtos.ToList()
            : dtos.Select(d => d with
            {
                StartValue = null,
                Progress = d.Progress is null ? null : d.Progress with { Current = null },
            }).ToList();
    }

    /// <summary>
    /// Re-checks the member's active goals after new data was logged. Marks a body-weight goal
    /// as achieved once the latest weight reaches the target, and, when <paramref name="workoutDate"/>
    /// is given, checks the weekly workout target for that week. Goals shared with friends get
    /// a feed post when that happens.
    /// </summary>
    public async Task EvaluateAsync(Guid userId, DateOnly? workoutDate, CancellationToken ct)
    {
        await EvaluateWeightGoalAsync(userId, ct);
        if (workoutDate is { } date)
        {
            await EvaluateWeeklyGoalAsync(userId, date, ct);
        }
    }

    private async Task EvaluateWeightGoalAsync(Guid userId, CancellationToken ct)
    {
        var goal = await db.Goals.SingleOrDefaultAsync(
            g => g.UserId == userId && g.Metric == GoalMetric.BodyWeight && g.Status == GoalStatus.Active, ct);
        if (goal is null || await LatestWeightAsync(userId, ct) is not { } weight)
        {
            return;
        }

        // A goal created before the first weigh-in starts from the first measurement
        goal.StartValue ??= weight;

        var reached = GoalMath.IsWeightReached(goal.StartValue, goal.TargetValue, weight);
        if (reached)
        {
            goal.Status = GoalStatus.Achieved;
            goal.AchievedAt = time.GetUtcNow().UtcDateTime;
        }

        await db.SaveChangesAsync(ct);

        if (reached && goal.Visibility == Visibility.Friends)
        {
            await feed.PublishAsync(new ActivityPost
            {
                UserId = userId,
                Type = PostType.GoalAchieved,
                GoalId = goal.Id,
                Summary = PostSummaries.WeightGoalReached(goal.TargetValue),
            }, ct);
        }
    }

    private async Task EvaluateWeeklyGoalAsync(Guid userId, DateOnly workoutDate, CancellationToken ct)
    {
        var goal = await db.Goals.AsNoTracking().SingleOrDefaultAsync(
            g => g.UserId == userId && g.Metric == GoalMetric.WorkoutsPerWeek && g.Status == GoalStatus.Active, ct);
        if (goal is not { Visibility: Visibility.Friends })
        {
            return;
        }

        var tz = (await members.GetAsync(userId, ct)).TimeZone;
        var weekStart = TimeZones.WeekStart(workoutDate);
        var from = TimeZones.StartOfDayUtc(weekStart, tz);
        var to = TimeZones.StartOfDayUtc(weekStart.AddDays(7), tz);
        var count = await db.Workouts.CountAsync(w => w.UserId == userId && w.PerformedAt >= from && w.PerformedAt < to, ct);

        // Posted once per week: the unique index on (GoalId, WeekStart) backs this check up
        if (count >= goal.TargetValue && !await feed.HasWeeklyPostAsync(goal.Id, weekStart, ct))
        {
            await feed.PublishAsync(new ActivityPost
            {
                UserId = userId,
                Type = PostType.WeeklyTargetMet,
                GoalId = goal.Id,
                WeekStart = weekStart,
                Summary = PostSummaries.WeeklyTargetMet(goal.TargetValue),
            }, ct);
        }
    }

    private static void ValidateTarget(Validator v, GoalMetric metric, decimal? target, DateOnly? targetDate, DateOnly today)
    {
        var (min, max) = GoalRules.TargetRange(metric);
        v.Range("targetValue", target, min, max);

        if (metric == GoalMetric.WorkoutsPerWeek && target is { } t && t != decimal.Truncate(t))
        {
            v.Add("targetValue", "Must be a whole number.");
        }

        if (targetDate is { } date)
        {
            if (metric != GoalMetric.BodyWeight)
            {
                v.Add("targetDate", "Only a body-weight goal has a target date.");
            }
            else if (date <= today)
            {
                v.Add("targetDate", "Must be in the future.");
            }
        }
    }

    private async Task<decimal?> LatestWeightAsync(Guid userId, CancellationToken ct) =>
        await db.BodyMeasurements
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.Date)
            .Select(m => (decimal?)m.WeightKg)
            .FirstOrDefaultAsync(ct);

    private async Task<Goal> LoadOwnAsync(Guid userId, Guid id, CancellationToken ct) =>
        await db.Goals.SingleOrDefaultAsync(g => g.Id == id && g.UserId == userId, ct) ?? throw new NotFoundException();

    /// <summary>The logged data that goal progress is computed from.</summary>
    private sealed record ProgressInputs(decimal? LatestWeight, int WorkoutsThisWeek, DailyTotalsDto? Today);

    private async Task<ProgressInputs> LoadProgressInputsAsync(Guid ownerId, CancellationToken ct)
    {
        var tz = (await members.GetAsync(ownerId, ct)).TimeZone;
        var today = TimeZones.Today(time, tz);

        var weekStart = TimeZones.StartOfDayUtc(TimeZones.WeekStart(today), tz);
        var weekEnd = TimeZones.StartOfDayUtc(TimeZones.WeekStart(today).AddDays(7), tz);
        var workoutsThisWeek = await db.Workouts
            .CountAsync(w => w.UserId == ownerId && w.PerformedAt >= weekStart && w.PerformedAt < weekEnd, ct);

        var todaysMeals = await MealService.MealsBetween(db.Meals.AsNoTracking().Where(m => m.UserId == ownerId), today, today, tz)
            .ToListAsync(ct);

        return new ProgressInputs(
            await LatestWeightAsync(ownerId, ct),
            workoutsThisWeek,
            Nutrition.DailyTotals(todaysMeals, tz).FirstOrDefault());
    }

    private static GoalProgressDto? Progress(Goal goal, ProgressInputs inputs)
    {
        if (goal.Status == GoalStatus.Archived)
        {
            return null;
        }

        switch (goal.Metric)
        {
            case GoalMetric.BodyWeight:
                var reached = goal.Status == GoalStatus.Achieved
                    || (inputs.LatestWeight is { } weight && GoalMath.IsWeightReached(goal.StartValue, goal.TargetValue, weight));
                return new GoalProgressDto(
                    inputs.LatestWeight,
                    reached ? 100 : GoalMath.WeightPercent(goal.StartValue, goal.TargetValue, inputs.LatestWeight),
                    reached,
                    "latest");

            case GoalMetric.WorkoutsPerWeek:
                return new GoalProgressDto(
                    inputs.WorkoutsThisWeek,
                    GoalMath.RatioPercent(inputs.WorkoutsThisWeek, goal.TargetValue),
                    inputs.WorkoutsThisWeek >= goal.TargetValue,
                    "thisWeek");

            case GoalMetric.DailyCalories:
                var calories = inputs.Today?.Calories;
                return new GoalProgressDto(
                    calories,
                    GoalMath.RatioPercent(calories ?? 0, goal.TargetValue),
                    calories is { } c && GoalMath.IsCalorieTargetMet(c, goal.TargetValue),
                    "today");

            case GoalMetric.DailyProtein:
                var protein = inputs.Today?.ProteinG;
                return new GoalProgressDto(
                    protein,
                    GoalMath.RatioPercent(protein ?? 0, goal.TargetValue),
                    protein >= goal.TargetValue,
                    "today");

            default:
                return null;
        }
    }

    private static GoalDto ToDto(Goal g, ProgressInputs inputs) => new(
        g.Id, g.UserId, g.Metric, g.TargetValue, g.StartValue, g.StartDate, g.TargetDate, g.Status, g.AchievedAt, g.Visibility,
        Progress(g, inputs));
}
