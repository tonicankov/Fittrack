using FitTrack.Api.Domain;

namespace FitTrack.Api.Features.Goals;

/// <summary>Allowed target ranges per kind of goal.</summary>
public static class GoalRules
{
    public static (decimal Min, decimal Max) TargetRange(GoalMetric metric) => metric switch
    {
        GoalMetric.BodyWeight => (20, 400),
        GoalMetric.WorkoutsPerWeek => (1, 14),
        GoalMetric.DailyCalories => (500, 10_000),
        GoalMetric.DailyProtein => (10, 1000),
        _ => throw new ArgumentOutOfRangeException(nameof(metric)),
    };

    public static string Unit(GoalMetric metric) => metric switch
    {
        GoalMetric.BodyWeight => "kg",
        GoalMetric.WorkoutsPerWeek => "workouts",
        GoalMetric.DailyCalories => "kcal",
        GoalMetric.DailyProtein => "g protein",
        _ => "",
    };
}
