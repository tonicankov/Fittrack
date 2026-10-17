using System.Text.Json.Serialization;
using FitTrack.Api.Common;
using FitTrack.Api.Domain;
using FitTrack.Api.Features.Feed;
using FitTrack.Api.Features.Goals;
using FitTrack.Api.Features.Measurements;
using FitTrack.Api.Features.Workouts;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace FitTrack.Api.Tests;

[Collection(ApiCollection.Name)]
public class FeedTests(ApiFactory factory)
{
    [Fact]
    public async Task Shared_workouts_appear_in_friends_feeds_and_private_ones_do_not()
    {
        var toni = await factory.RegisterAsync("Toni");
        var nadia = await factory.RegisterAsync("Nadia", invitedBy: toni);

        await toni.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout("Leg day", visibility: "Friends"));
        await toni.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout("Secret cardio", visibility: "Private"));

        var feed = await nadia.Client.GetAsync<Page<PostDto>>("/api/feed");

        var post = Assert.Single(feed.Items);
        Assert.Equal("Toni", post.AuthorName);
        Assert.Equal(PostType.WorkoutLogged, post.Type);
        Assert.Equal("Leg day: 2 exercises, 45 min", post.Summary);
    }

    [Fact]
    public async Task Making_a_workout_private_removes_its_post()
    {
        var toni = await factory.RegisterAsync();
        var nadia = await factory.RegisterAsync(invitedBy: toni);
        var workout = await toni.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout(visibility: "Friends"));

        await toni.Client.PutAsync<WorkoutDto>($"/api/workouts/{workout.Id}", TestData.Workout(visibility: "Private"));

        Assert.Empty((await nadia.Client.GetAsync<Page<PostDto>>("/api/feed")).Items);
    }

    [Fact]
    public async Task Strangers_do_not_see_your_posts()
    {
        var toni = await factory.RegisterAsync();
        var stranger = await factory.RegisterAsync();
        await toni.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout(visibility: "Friends"));

        Assert.Empty((await stranger.Client.GetAsync<Page<PostDto>>("/api/feed")).Items);
    }

    [Fact]
    public async Task Meeting_the_weekly_target_is_posted_once_per_week()
    {
        var toni = await factory.RegisterAsync();
        await toni.Client.PostAsync<GoalDto>("/api/goals", new { metric = "WorkoutsPerWeek", targetValue = 1, visibility = "Friends" });

        await toni.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout(visibility: "Private"));
        await toni.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout(visibility: "Private"));

        var feed = await toni.Client.GetAsync<Page<PostDto>>("/api/feed");
        var weekly = Assert.Single(feed.Items);
        Assert.Equal(PostType.WeeklyTargetMet, weekly.Type);
    }

    [Fact]
    public async Task Reaching_a_shared_weight_goal_is_posted()
    {
        var toni = await factory.RegisterAsync();
        var nadia = await factory.RegisterAsync(invitedBy: toni);
        await toni.Client.PutAsync<MeasurementDto>($"/api/measurements/{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}", new { weightKg = 70m });
        await toni.Client.PostAsync<GoalDto>("/api/goals", new { metric = "BodyWeight", targetValue = 72, visibility = "Friends" });

        await toni.Client.PutAsync<MeasurementDto>($"/api/measurements/{DateTime.UtcNow:yyyy-MM-dd}", new { weightKg = 72.2m, visibility = "Private" });

        var post = Assert.Single((await nadia.Client.GetAsync<Page<PostDto>>("/api/feed")).Items);
        Assert.Equal(PostType.GoalAchieved, post.Type);
        Assert.Equal("Reached the target weight of 72 kg", post.Summary);
    }

    [Fact]
    public async Task Friends_receive_new_posts_in_real_time()
    {
        var toni = await factory.RegisterAsync("Toni");
        var nadia = await factory.RegisterAsync("Nadia", invitedBy: toni);

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "hubs/feed"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(nadia.Token);
                options.Transports = HttpTransportType.LongPolling;
            })
            .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

        var received = new TaskCompletionSource<PostDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<PostDto>("PostCreated", post => received.TrySetResult(post));
        await connection.StartAsync();
        await Task.Delay(500); // let the server put the connection in Nadia's group

        await toni.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout("Push day", visibility: "Friends"));

        var live = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("Toni", live.AuthorName);
        Assert.StartsWith("Push day", live.Summary);
    }
}
