using FitTrack.Api.Common;
using FitTrack.Api.Domain;

namespace FitTrack.Api.Features.Meals;

public static class Nutrition
{
    /// <summary>
    /// Totals per local calendar day. Days without meals are left out: "nothing logged"
    /// is not the same as "ate nothing".
    /// </summary>
    public static List<DailyTotalsDto> DailyTotals(IEnumerable<Meal> meals, TimeZoneInfo tz) =>
        meals
            .GroupBy(m => TimeZones.LocalDate(m.EatenAt, tz))
            .OrderBy(g => g.Key)
            .Select(g => new DailyTotalsDto(
                g.Key,
                g.Count(),
                g.Sum(m => m.Calories),
                g.Sum(m => m.ProteinG ?? 0),
                g.Sum(m => m.CarbsG ?? 0),
                g.Sum(m => m.FatG ?? 0)))
            .ToList();
}
