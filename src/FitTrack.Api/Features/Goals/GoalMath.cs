namespace FitTrack.Api.Features.Goals;

/// <summary>The progress rules from the specification, kept free of I/O so they are easy to test.</summary>
public static class GoalMath
{
    /// <summary>
    /// A body-weight goal is reached when the weight reaches or passes the target in the
    /// direction from the start weight to the target (losing or gaining).
    /// </summary>
    public static bool IsWeightReached(decimal? start, decimal target, decimal current)
    {
        if (start is null || start == target)
        {
            return current == target;
        }

        return start > target ? current <= target : current >= target;
    }

    /// <summary>(start − current) / (start − target), as 0–100%.</summary>
    public static decimal WeightPercent(decimal? start, decimal target, decimal? current)
    {
        if (current is null)
        {
            return 0;
        }

        if (start is null || start == target)
        {
            return IsWeightReached(start, target, current.Value) ? 100 : 0;
        }

        return Clamp((start.Value - current.Value) / (start.Value - target) * 100);
    }

    /// <summary>How much of a "reach at least" target is done, as 0–100%.</summary>
    public static decimal RatioPercent(decimal current, decimal target) =>
        target <= 0 ? 0 : Clamp(current / target * 100);

    /// <summary>A calorie target is met when the day's calories are within ±10% of it.</summary>
    public static bool IsCalorieTargetMet(decimal calories, decimal target) =>
        Math.Abs(calories - target) <= target * 0.10m;

    private static decimal Clamp(decimal percent) => Math.Round(Math.Clamp(percent, 0, 100), 1);
}
