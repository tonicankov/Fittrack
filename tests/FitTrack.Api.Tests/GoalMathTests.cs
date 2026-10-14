using FitTrack.Api.Features.Goals;

namespace FitTrack.Api.Tests;

public class GoalMathTests
{
    [Theory]
    [InlineData(80, 75, 76, false)] // losing, not there yet
    [InlineData(80, 75, 75, true)]
    [InlineData(80, 75, 74.5, true)] // passed the target
    [InlineData(60, 65, 66, true)] // gaining
    [InlineData(60, 65, 64, false)]
    public void Weight_goals_respect_the_direction(decimal start, decimal target, decimal current, bool reached)
    {
        Assert.Equal(reached, GoalMath.IsWeightReached(start, target, current));
    }

    [Theory]
    [InlineData(80, 75, 77.5, 50)]
    [InlineData(80, 75, 82, 0)] // moved away from the target
    [InlineData(80, 75, 70, 100)] // capped
    public void Weight_progress_is_the_share_of_the_way_done(decimal start, decimal target, decimal current, decimal percent)
    {
        Assert.Equal(percent, GoalMath.WeightPercent(start, target, current));
    }

    [Theory]
    [InlineData(2000, 2200, true)] // within 10%
    [InlineData(2420, 2200, true)]
    [InlineData(1900, 2200, false)]
    [InlineData(2500, 2200, false)]
    public void Calorie_targets_allow_ten_percent(decimal calories, decimal target, bool met)
    {
        Assert.Equal(met, GoalMath.IsCalorieTargetMet(calories, target));
    }

    [Fact]
    public void Ratio_progress_is_capped_at_100()
    {
        Assert.Equal(100m, GoalMath.RatioPercent(5, 3));
        Assert.Equal(33.3m, GoalMath.RatioPercent(1, 3));
    }
}
