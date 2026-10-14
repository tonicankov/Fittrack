using System.Net;
using System.Net.Http.Json;
using FitTrack.Api.Domain;
using FitTrack.Api.Features.Goals;
using FitTrack.Api.Features.Measurements;
using FitTrack.Api.Features.Workouts;

namespace FitTrack.Api.Tests;

[Collection(ApiCollection.Name)]
public class GoalTests(ApiFactory factory)
{
    private static string Day(int daysAgo) => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-daysAgo)).ToString("yyyy-MM-dd");

    [Fact]
    public async Task Only_one_active_goal_of_each_kind()
    {
        var member = await factory.RegisterAsync();
        await member.Client.PostAsync<GoalDto>("/api/goals", new { metric = "DailyProtein", targetValue = 140 });

        var second = await member.Client.PostAsJsonAsync("/api/goals", new { metric = "DailyProtein", targetValue = 150 });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task A_weight_goal_is_achieved_when_a_measurement_reaches_the_target()
    {
        var member = await factory.RegisterAsync();
        await member.Client.PutAsync<MeasurementDto>($"/api/measurements/{Day(3)}", new { weightKg = 80m });
        var goal = await member.Client.PostAsync<GoalDto>("/api/goals", new { metric = "BodyWeight", targetValue = 78 });
        Assert.Equal(80m, goal.StartValue);

        await member.Client.PutAsync<MeasurementDto>($"/api/measurements/{Day(1)}", new { weightKg = 79m });
        var halfway = Assert.Single(await member.Client.GetAsync<List<GoalDto>>("/api/goals"));
        Assert.Equal(GoalStatus.Active, halfway.Status);
        Assert.Equal(50m, halfway.Progress!.Percent);

        await member.Client.PutAsync<MeasurementDto>($"/api/measurements/{Day(0)}", new { weightKg = 77.9m });
        var done = Assert.Single(await member.Client.GetAsync<List<GoalDto>>("/api/goals"));
        Assert.Equal(GoalStatus.Achieved, done.Status);
        Assert.NotNull(done.AchievedAt);
    }

    [Fact]
    public async Task Weekly_workout_progress_counts_this_weeks_workouts()
    {
        var member = await factory.RegisterAsync();
        await member.Client.PostAsync<GoalDto>("/api/goals", new { metric = "WorkoutsPerWeek", targetValue = 2 });

        await member.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout());

        var goal = Assert.Single(await member.Client.GetAsync<List<GoalDto>>("/api/goals"));
        Assert.Equal(1m, goal.Progress!.Current);
        Assert.Equal(50m, goal.Progress.Percent);
        Assert.False(goal.Progress.IsMet);
    }

    [Fact]
    public async Task Archived_goals_make_room_for_a_new_one()
    {
        var member = await factory.RegisterAsync();
        var first = await member.Client.PostAsync<GoalDto>("/api/goals", new { metric = "DailyCalories", targetValue = 2200 });

        await member.Client.PutAsync<GoalDto>($"/api/goals/{first.Id}", new { status = "Archived" });
        var second = await member.Client.PostAsJsonAsync("/api/goals", new { metric = "DailyCalories", targetValue = 2400 });

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }
}
