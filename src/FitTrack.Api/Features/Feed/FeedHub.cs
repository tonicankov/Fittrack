using FitTrack.Api.Auth;
using Microsoft.AspNetCore.SignalR;

namespace FitTrack.Api.Features.Feed;

/// <summary>
/// Clients only listen on this hub; they never call it. Each connection joins a group named
/// after its member, so the server can reach every open tab and device of that member.
/// </summary>
public sealed class FeedHub : Hub
{
    public const string Path = "/hubs/feed";

    public static string GroupFor(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        if (Context.User is { } user)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(user.GetUserId()));
        }

        await base.OnConnectedAsync();
    }
}
