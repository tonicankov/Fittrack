using System.Net;
using FitTrack.Api.Features.Goals;
using FitTrack.Api.Features.Meals;
using FitTrack.Api.Features.Measurements;
using FitTrack.Api.Features.Stats;
using FitTrack.Api.Features.Workouts;

namespace FitTrack.Api.Tests;

[Collection(ApiCollection.Name)]
public class StatsTests(ApiFactory factory)
{
    private static string Day(int daysAgo) => $"{DateTime.UtcNow.AddDays(-daysAgo):yyyy-MM-dd}";

    [Fact]
    public async Task Weight_trend_lists_measurements_oldest_first_with_the_target()
    {
        var member = await factory.RegisterAsync();
        await member.Client.PutAsync<MeasurementDto>($"/api/measurements/{Day(10)}", new { weightKg = 81m });
        await member.Client.PutAsync<MeasurementDto>($"/api/measurements/{Day(5)}", new { weightKg = 80m });
        await member.Client.PutAsync<MeasurementDto>($"/api/measurements/{Day(200)}", new { weightKg = 85m });
        await member.Client.PostAsync<GoalDto>("/api/goals", new { metric = "BodyWeight", targetValue = 76 });

        var trend = await member.Client.GetAsync<WeightTrendDto>("/api/stats/weight?range=30d");

        Assert.Equal(new[] { 81m, 80m }, trend.Points.Select(p => p.WeightKg));
        Assert.Equal(76m, trend.TargetKg);
    }

    [Fact]
    public async Task Workouts_per_week_cover_twelve_weeks_ending_this_week()
    {
        var member = await factory.RegisterAsync();
        await member.Client.PostAsync<GoalDto>("/api/goals", new { metric = "WorkoutsPerWeek", targetValue = 3 });
        await member.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout());
        await member.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout());

        var stats = await member.Client.GetAsync<WorkoutsPerWeekDto>("/api/stats/workouts-per-week");

        Assert.Equal(12, stats.Weeks.Count);
        Assert.Equal(2, stats.Weeks[^1].Workouts);
        Assert.Equal(0, stats.Weeks[0].Workouts);
        Assert.Equal(3m, stats.Target);
    }

    [Fact]
    public async Task Volume_counts_strength_sets_only()
    {
        var member = await factory.RegisterAsync();
        await member.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout()); // 5×80 + 5×85, plus a bike ride

        var volume = await member.Client.GetAsync<List<WeekVolumeDto>>("/api/stats/volume?weeks=4");

        Assert.Equal(4, volume.Count);
        Assert.Equal(825m, volume[^1].VolumeKg);
    }

    [Fact]
    public async Task Days_without_meals_are_gaps_not_zeros()
    {
        var member = await factory.RegisterAsync();
        await member.Client.PostAsync<MealDto>("/api/meals", TestData.Meal("Lunch", 650, 35));

        var nutrition = await member.Client.GetAsync<NutritionDto>("/api/stats/nutrition?days=7");

        Assert.Equal(7, nutrition.Days.Count);
        Assert.Equal(650, nutrition.Days[^1].Calories);
        Assert.All(nutrition.Days.Take(6), d => Assert.Null(d.Calories));
    }

    [Fact]
    public async Task An_unknown_range_is_rejected()
    {
        var member = await factory.RegisterAsync();

        var response = await member.Client.GetAsync("/api/stats/weight?range=5y");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
