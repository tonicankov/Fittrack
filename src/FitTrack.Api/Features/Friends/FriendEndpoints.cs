using System.Security.Claims;
using FitTrack.Api.Auth;

namespace FitTrack.Api.Features.Friends;

public static class FriendEndpoints
{
    public static IEndpointRouteBuilder MapFriendEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/friends").WithTags("Friends");

        group.MapGet("", async (ClaimsPrincipal user, FriendshipService friends, CancellationToken ct) =>
            Results.Ok(await friends.ListFriendsAsync(user.GetUserId(), ct)));

        group.MapDelete("/{userId:guid}", async (Guid userId, ClaimsPrincipal user, FriendshipService friends, CancellationToken ct) =>
        {
            await friends.RemoveFriendAsync(user.GetUserId(), userId, ct);
            return Results.NoContent();
        });

        group.MapGet("/requests", async (ClaimsPrincipal user, FriendshipService friends, CancellationToken ct) =>
            Results.Ok(await friends.ListRequestsAsync(user.GetUserId(), ct)));

        group.MapPost("/requests", async (SendFriendRequest request, ClaimsPrincipal user, FriendshipService friends, CancellationToken ct) =>
        {
            var sent = await friends.SendRequestAsync(user.GetUserId(), request, ct);
            return Results.Created($"/api/friends/requests/{sent.Id}", sent);
        });

        group.MapPost("/requests/{id:guid}/accept", async (Guid id, ClaimsPrincipal user, FriendshipService friends, CancellationToken ct) =>
            Results.Ok(await friends.AcceptAsync(user.GetUserId(), id, ct)));

        // The receiver declines, the sender cancels
        group.MapDelete("/requests/{id:guid}", async (Guid id, ClaimsPrincipal user, FriendshipService friends, CancellationToken ct) =>
        {
            await friends.DeleteRequestAsync(user.GetUserId(), id, ct);
            return Results.NoContent();
        });

        // A friend's profile; their shared entries come from the list endpoints with ?userId=
        app.MapGet("/api/users/{id:guid}", async (Guid id, ClaimsPrincipal user, FriendshipService friends, CancellationToken ct) =>
                Results.Ok(await friends.GetProfileAsync(user.GetUserId(), id, ct)))
            .WithTags("Friends");

        return app;
    }
}
