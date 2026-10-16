using FitTrack.Api.Features.Friends;
using Microsoft.AspNetCore.SignalR;

namespace FitTrack.Api.Features.Feed;

public sealed class SignalRFeedNotifier(IHubContext<FeedHub> hub) : IFeedNotifier
{
    public Task PostCreatedAsync(IReadOnlyCollection<Guid> recipientIds, PostDto post, CancellationToken ct) =>
        Groups(recipientIds).SendAsync("PostCreated", post, ct);

    public Task PostRemovedAsync(IReadOnlyCollection<Guid> recipientIds, Guid postId, CancellationToken ct) =>
        Groups(recipientIds).SendAsync("PostRemoved", postId, ct);

    public Task FriendRequestReceivedAsync(Guid userId, FriendRequestDto request, CancellationToken ct) =>
        hub.Clients.Group(FeedHub.GroupFor(userId)).SendAsync("FriendRequestReceived", request, ct);

    public Task FriendRequestAcceptedAsync(Guid userId, FriendDto friend, CancellationToken ct) =>
        hub.Clients.Group(FeedHub.GroupFor(userId)).SendAsync("FriendRequestAccepted", friend, ct);

    private IClientProxy Groups(IReadOnlyCollection<Guid> userIds) =>
        hub.Clients.Groups(userIds.Select(FeedHub.GroupFor).ToList());
}
