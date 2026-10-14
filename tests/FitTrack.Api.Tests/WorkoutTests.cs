using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitTrack.Api.Common;
using FitTrack.Api.Domain;
using FitTrack.Api.Features.Workouts;

namespace FitTrack.Api.Tests;

[Collection(ApiCollection.Name)]
public class WorkoutTests(ApiFactory factory)
{
    [Fact]
    public async Task A_logged_workout_keeps_its_exercises_and_sets_in_order()
    {
        var member = await factory.RegisterAsync();

        var workout = await member.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout());

        Assert.Equal(new[] { "Squat", "Bike" }, workout.Exercises.Select(x => x.Name).ToArray());
        Assert.Equal(new[] { 80m, 85m }, workout.Exercises[0].Sets.Select(s => s.WeightKg));
        Assert.Empty(workout.Exercises[1].Sets);
        Assert.Equal(600, workout.Exercises[1].DurationSeconds);
        Assert.Equal(Visibility.Friends, workout.Visibility); // the profile default
    }

    [Fact]
    public async Task Invalid_workouts_report_every_problem()
    {
        var member = await factory.RegisterAsync();

        var response = await member.Client.PostAsJsonAsync("/api/workouts", new
        {
            name = "",
            durationMinutes = 0,
            exercises = new object[] { new { name = "Bench press", kind = "Strength", sets = Array.Empty<object>() } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = problem.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("name", out _));
        Assert.True(errors.TryGetProperty("durationMinutes", out _));
        Assert.True(errors.TryGetProperty("exercises[0].sets", out _));
    }

    [Fact]
    public async Task Editing_a_workout_replaces_its_exercises()
    {
        var member = await factory.RegisterAsync();
        var workout = await member.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout());

        var edited = await member.Client.PutAsync<WorkoutDto>($"/api/workouts/{workout.Id}", new
        {
            name = "Leg day (light)",
            durationMinutes = 30,
            visibility = "Private",
            exercises = new[] { new { name = "Lunge", kind = "Strength", sets = new[] { new { reps = 10, weightKg = 20m } } } },
        });

        Assert.Equal("Leg day (light)", edited.Name);
        Assert.Equal(Visibility.Private, edited.Visibility);
        var lunge = Assert.Single(edited.Exercises);
        Assert.Equal("Lunge", lunge.Name);

        var reloaded = await member.Client.GetAsync<WorkoutDto>($"/api/workouts/{workout.Id}");
        Assert.Single(reloaded.Exercises);
    }

    [Fact]
    public async Task A_deleted_workout_is_gone()
    {
        var member = await factory.RegisterAsync();
        var workout = await member.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout());

        var delete = await member.Client.DeleteAsync($"/api/workouts/{workout.Id}");
        var get = await member.Client.GetAsync($"/api/workouts/{workout.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Workouts_are_listed_newest_first()
    {
        var member = await factory.RegisterAsync();
        await member.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout("Older", performedAt: DateTimeOffset.UtcNow.AddDays(-2)));
        await member.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout("Newer", performedAt: DateTimeOffset.UtcNow.AddHours(-1)));

        var page = await member.Client.GetAsync<Page<WorkoutDto>>("/api/workouts");

        Assert.Equal(new[] { "Newer", "Older" }, page.Items.Select(w => w.Name).ToArray());
        Assert.Null(page.NextBefore);
    }

    [Fact]
    public async Task Exercise_names_are_suggested_from_earlier_workouts()
    {
        var member = await factory.RegisterAsync();
        await member.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout());

        var suggestions = await member.Client.GetAsync<List<string>>("/api/exercises/suggestions?q=sq");

        Assert.Equal(new[] { "Squat" }, suggestions);
    }
}
