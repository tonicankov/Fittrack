using System.Linq.Expressions;
using FitTrack.Api.Common;
using FitTrack.Api.Data;
using FitTrack.Api.Domain;
using FitTrack.Api.Features.Meals;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Features.Stats;

/// <summary>
/// The series behind the progress dashboard. Always computed from the member's own entries
/// (private ones included) and never shown to anyone else, so it takes no owner id.
/// </summary>
public sealed class StatsService(AppDbContext db, MemberSettingsProvider members, TimeProvider time)
{
    public static readonly string[] WeightRanges = ["30d", "90d", "1y", "all"];

    public async Task<WeightTrendDto> GetWeightTrendAsync(Guid userId, string? range, CancellationToken ct)
    {
        range ??= "90d";
        if (!WeightRanges.Contains(range))
        {
            throw RequestValidationException.For("range", $"Use one of: {string.Join(", ", WeightRanges)}.");
        }

        var today = await TodayAsync(userId, ct);
        DateOnly? from = range switch
        {
            "30d" => today.AddDays(-29),
            "90d" => today.AddDays(-89),
            "1y" => today.AddYears(-1),
            _ => null,
        };

        var query = db.BodyMeasurements.AsNoTracking().Where(m => m.UserId == userId);
        if (from is not null)
        {
            query = query.Where(m => m.Date >= from);
        }

        var points = await query
            .OrderBy(m => m.Date)
            .Select(m => new WeightPoint(m.Date, m.WeightKg))
            .ToListAsync(ct);

        return new WeightTrendDto(points, await ActiveTargetAsync(userId, GoalMetric.BodyWeight, ct));
    }

    public async Task<WorkoutsPerWeekDto> GetWorkoutsPerWeekAsync(Guid userId, int? weeks, CancellationToken ct)
    {
        var (tz, weekStarts) = await WeeksAsync(userId, weeks, ct);
        var performed = await db.Workouts.AsNoTracking()
            .Where(w => w.UserId == userId)
            .Where(InWeeks(weekStarts, tz))
            .Select(w => w.PerformedAt)
            .ToListAsync(ct);

        var counts = performed
            .GroupBy(p => TimeZones.WeekStart(TimeZones.LocalDate(p, tz)))
            .ToDictionary(g => g.Key, g => g.Count());

        return new WorkoutsPerWeekDto(
            weekStarts.Select(w => new WeekCountDto(w, counts.GetValueOrDefault(w))).ToList(),
            await ActiveTargetAsync(userId, GoalMetric.WorkoutsPerWeek, ct));
    }

    /// <summary>Sum of reps × weight over all strength sets, per week.</summary>
    public async Task<List<WeekVolumeDto>> GetWeeklyVolumeAsync(Guid userId, int? weeks, CancellationToken ct)
    {
        var (tz, weekStarts) = await WeeksAsync(userId, weeks, ct);
        var workouts = await db.Workouts.AsNoTracking()
            .Include(w => w.Exercises).ThenInclude(x => x.Sets)
            .Where(w => w.UserId == userId)
            .Where(InWeeks(weekStarts, tz))
            .ToListAsync(ct);

        var volumes = workouts
            .GroupBy(w => TimeZones.WeekStart(TimeZones.LocalDate(w.PerformedAt, tz)))
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(w => w.Exercises)
                      .Where(x => x.Kind == ExerciseKind.Strength)
                      .SelectMany(x => x.Sets)
                      .Sum(s => s.Reps * s.WeightKg));

        return weekStarts.Select(w => new WeekVolumeDto(w, volumes.GetValueOrDefault(w))).ToList();
    }

    public async Task<NutritionDto> GetDailyNutritionAsync(Guid userId, int? days, CancellationToken ct)
    {
        var count = days ?? 7;
        if (count is < 1 or > 90)
        {
            throw RequestValidationException.For("days", "Must be between 1 and 90.");
        }

        var tz = (await members.GetAsync(userId, ct)).TimeZone;
        var today = TimeZones.Today(time, tz);
        var first = today.AddDays(-(count - 1));

        var meals = await MealService.MealsBetween(db.Meals.AsNoTracking().Where(m => m.UserId == userId), first, today, tz)
            .ToListAsync(ct);
        var totals = Nutrition.DailyTotals(meals, tz).ToDictionary(t => t.Date);

        var series = Enumerable.Range(0, count)
            .Select(i => first.AddDays(i))
            .Select(d => totals.TryGetValue(d, out var t)
                ? new NutritionDayDto(d, t.Calories, t.ProteinG)
                : new NutritionDayDto(d, null, null)) // a gap, not zero
            .ToList();

        return new NutritionDto(
            series,
            await ActiveTargetAsync(userId, GoalMetric.DailyCalories, ct),
            await ActiveTargetAsync(userId, GoalMetric.DailyProtein, ct));
    }

    private async Task<DateOnly> TodayAsync(Guid userId, CancellationToken ct) =>
        TimeZones.Today(time, (await members.GetAsync(userId, ct)).TimeZone);

    /// <summary>The Mondays of the last <paramref name="weeks"/> weeks, oldest first, ending with this week.</summary>
    private async Task<(TimeZoneInfo Tz, List<DateOnly> WeekStarts)> WeeksAsync(Guid userId, int? weeks, CancellationToken ct)
    {
        var count = weeks ?? 12;
        if (count is < 1 or > 104)
        {
            throw RequestValidationException.For("weeks", "Must be between 1 and 104.");
        }

        var tz = (await members.GetAsync(userId, ct)).TimeZone;
        var thisWeek = TimeZones.WeekStart(TimeZones.Today(time, tz));
        var starts = Enumerable.Range(0, count).Select(i => thisWeek.AddDays(-7 * (count - 1 - i))).ToList();
        return (tz, starts);
    }

    private static Expression<Func<Workout, bool>> InWeeks(List<DateOnly> weekStarts, TimeZoneInfo tz)
    {
        var from = TimeZones.StartOfDayUtc(weekStarts[0], tz);
        var to = TimeZones.StartOfDayUtc(weekStarts[^1].AddDays(7), tz);
        return w => w.PerformedAt >= from && w.PerformedAt < to;
    }

    private Task<decimal?> ActiveTargetAsync(Guid userId, GoalMetric metric, CancellationToken ct) =>
        db.Goals
            .Where(g => g.UserId == userId && g.Metric == metric && g.Status == GoalStatus.Active)
            .Select(g => (decimal?)g.TargetValue)
            .FirstOrDefaultAsync(ct);
}
