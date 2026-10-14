namespace FitTrack.Api.Features.Friends;

public sealed record FriendDto(Guid UserId, string DisplayName, DateTime Since);

/// <param name="UserId">The other member: the receiver of an outgoing request, the sender of an incoming one.</param>
/// <param name="Direction">"incoming" or "outgoing".</param>
public sealed record FriendRequestDto(Guid Id, Guid UserId, string DisplayName, string Direction, DateTime CreatedAt);

public sealed record SendFriendRequest(string? Email);
