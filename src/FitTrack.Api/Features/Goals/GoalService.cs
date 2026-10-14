using FitTrack.Api.Common;
using FitTrack.Api.Data;
using FitTrack.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Features.Goals;

public sealed class GoalService(AppDbContext db, MemberSettingsProvider members, TimeProvider time)
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

        return ToDto(goal);
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
        goal.Visibility = request.Visibility ?? goal.Visibility;
        if (request.Status is { } status)
        {
            goal.Status = status;
            goal.AchievedAt = status == GoalStatus.Active ? null : goal.AchievedAt;
        }

        await db.SaveChangesAsync(ct);
        return ToDto(goal);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken ct)
    {
        db.Goals.Remove(await LoadOwnAsync(userId, id, ct));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Active goals first, then the rest, newest first.</summary>
    public async Task<List<GoalDto>> ListAsync(Guid viewerId, Guid ownerId, CancellationToken ct)
    {
        if (viewerId != ownerId)
        {
            throw new NotFoundException();
        }

        var goals = await db.Goals.AsNoTracking()
            .Where(g => g.UserId == ownerId)
            .OrderBy(g => g.Status == GoalStatus.Active ? 0 : 1)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync(ct);

        return goals.Select(ToDto).ToList();
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

    private static GoalDto ToDto(Goal g) => new(
        g.Id, g.UserId, g.Metric, g.TargetValue, g.StartValue, g.StartDate, g.TargetDate, g.Status, g.AchievedAt, g.Visibility,
        Progress: null);
}
