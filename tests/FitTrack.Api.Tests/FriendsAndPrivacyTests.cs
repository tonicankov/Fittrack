using System.Net;
using System.Net.Http.Json;
using FitTrack.Api.Common;
using FitTrack.Api.Features.Friends;
using FitTrack.Api.Features.Goals;
using FitTrack.Api.Features.Measurements;
using FitTrack.Api.Features.Workouts;

namespace FitTrack.Api.Tests;

[Collection(ApiCollection.Name)]
public class FriendsAndPrivacyTests(ApiFactory factory)
{
    [Fact]
    public async Task Using_an_invite_makes_you_friends_with_the_inviter()
    {
        var toni = await factory.RegisterAsync("Toni");
        var nadia = await factory.RegisterAsync("Nadia", invitedBy: toni);

        var friendsOfToni = await toni.Client.GetAsync<List<FriendDto>>("/api/friends");

        Assert.Equal(nadia.Id, Assert.Single(friendsOfToni).UserId);
    }

    [Fact]
    public async Task A_friend_request_can_be_accepted()
    {
        var alex = await factory.RegisterAsync("Alex");
        var sam = await factory.RegisterAsync("Sam");

        await alex.Client.PostAsync<FriendRequestDto>("/api/friends/requests", new { email = sam.Email });
        var incoming = Assert.Single(await sam.Client.GetAsync<List<FriendRequestDto>>("/api/friends/requests"));
        Assert.Equal("incoming", incoming.Direction);
        Assert.Equal(alex.Id, incoming.UserId);

        await sam.Client.PostAsync<FriendDto>($"/api/friends/requests/{incoming.Id}/accept", new { });

        Assert.Equal(sam.Id, Assert.Single(await alex.Client.GetAsync<List<FriendDto>>("/api/friends")).UserId);
        Assert.Empty(await sam.Client.GetAsync<List<FriendRequestDto>>("/api/friends/requests"));
    }

    [Fact]
    public async Task A_second_request_to_the_same_member_conflicts()
    {
        var alex = await factory.RegisterAsync();
        var sam = await factory.RegisterAsync();
        await alex.Client.PostAsync<FriendRequestDto>("/api/friends/requests", new { email = sam.Email });

        var again = await alex.Client.PostAsJsonAsync("/api/friends/requests", new { email = sam.Email });
        var reverse = await sam.Client.PostAsJsonAsync("/api/friends/requests", new { email = alex.Email });

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, reverse.StatusCode);
    }

    [Fact]
    public async Task Only_the_receiver_can_accept_a_request()
    {
        var alex = await factory.RegisterAsync();
        var sam = await factory.RegisterAsync();
        var sent = await alex.Client.PostAsync<FriendRequestDto>("/api/friends/requests", new { email = sam.Email });

        var response = await alex.Client.PostAsJsonAsync($"/api/friends/requests/{sent.Id}/accept", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Friends_see_shared_entries_but_not_private_ones()
    {
        var toni = await factory.RegisterAsync("Toni");
        var nadia = await factory.RegisterAsync("Nadia", invitedBy: toni);
        var shared = await toni.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout("Shared", visibility: "Friends"));
        var hidden = await toni.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout("Hidden", visibility: "Private"));

        var page = await nadia.Client.GetAsync<Page<WorkoutDto>>($"/api/workouts?userId={toni.Id}");
        var privateOne = await nadia.Client.GetAsync($"/api/workouts/{hidden.Id}");
        var sharedOne = await nadia.Client.GetAsync($"/api/workouts/{shared.Id}");

        Assert.Equal("Shared", Assert.Single(page.Items).Name);
        Assert.Equal(HttpStatusCode.NotFound, privateOne.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sharedOne.StatusCode);
    }

    [Fact]
    public async Task Strangers_see_nothing()
    {
        var toni = await factory.RegisterAsync();
        var stranger = await factory.RegisterAsync();
        var shared = await toni.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout(visibility: "Friends"));

        var page = await stranger.Client.GetAsync<Page<WorkoutDto>>($"/api/workouts?userId={toni.Id}");
        var single = await stranger.Client.GetAsync($"/api/workouts/{shared.Id}");
        var profile = await stranger.Client.GetAsync($"/api/users/{toni.Id}");

        Assert.Empty(page.Items);
        Assert.Equal(HttpStatusCode.NotFound, single.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, profile.StatusCode);
    }

    [Fact]
    public async Task Removing_a_friend_hides_their_entries_again()
    {
        var toni = await factory.RegisterAsync();
        var nadia = await factory.RegisterAsync(invitedBy: toni);
        await toni.Client.PostAsync<WorkoutDto>("/api/workouts", TestData.Workout(visibility: "Friends"));

        var removed = await nadia.Client.DeleteAsync($"/api/friends/{toni.Id}");
        var page = await nadia.Client.GetAsync<Page<WorkoutDto>>($"/api/workouts?userId={toni.Id}");

        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task Friends_see_goal_progress_but_not_the_numbers_behind_it()
    {
        var toni = await factory.RegisterAsync();
        var nadia = await factory.RegisterAsync(invitedBy: toni);
        await toni.Client.PutAsync<MeasurementDto>($"/api/measurements/{DateTime.UtcNow.AddDays(-2):yyyy-MM-dd}",
            new { weightKg = 80m, visibility = "Private" });
        await toni.Client.PostAsync<GoalDto>("/api/goals", new { metric = "BodyWeight", targetValue = 76, visibility = "Friends" });
        await toni.Client.PutAsync<MeasurementDto>($"/api/measurements/{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}",
            new { weightKg = 78m, visibility = "Private" });

        var mine = Assert.Single(await toni.Client.GetAsync<List<GoalDto>>("/api/goals"));
        var theirs = Assert.Single(await nadia.Client.GetAsync<List<GoalDto>>($"/api/goals?userId={toni.Id}"));
        var measurements = await nadia.Client.GetAsync<List<MeasurementDto>>($"/api/measurements?userId={toni.Id}");

        Assert.Equal(78m, mine.Progress!.Current);
        Assert.Equal(50m, theirs.Progress!.Percent);
        Assert.Null(theirs.Progress.Current);
        Assert.Null(theirs.StartValue);
        Assert.Empty(measurements);
    }

    [Fact]
    public async Task Friends_can_open_each_others_profiles()
    {
        var toni = await factory.RegisterAsync("Toni");
        var nadia = await factory.RegisterAsync("Nadia", invitedBy: toni);

        var profile = await nadia.Client.GetAsync<UserProfileDto>($"/api/users/{toni.Id}");

        Assert.Equal("Toni", profile.DisplayName);
        Assert.True(profile.IsFriend);
        Assert.NotNull(profile.FriendsSince);
    }
}
