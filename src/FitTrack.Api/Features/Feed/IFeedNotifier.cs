using FitTrack.Api.Features.Friends;

namespace FitTrack.Api.Features.Feed;

/// <summary>
/// Delivers feed and friendship changes to members who have the app open.
/// The services only know this interface, so tests can replace real-time delivery.
/// </summary>
public interface IFeedNotifier
{
    Task PostCreatedAsync(IReadOnlyCollection<Guid> recipientIds, PostDto post, CancellationToken ct);

    Task PostRemovedAsync(IReadOnlyCollection<Guid> recipientIds, Guid postId, CancellationToken ct);

    Task FriendRequestReceivedAsync(Guid userId, FriendRequestDto request, CancellationToken ct);

    Task FriendRequestAcceptedAsync(Guid userId, FriendDto friend, CancellationToken ct);
}
