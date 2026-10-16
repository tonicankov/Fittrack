using FitTrack.Api.Common;
using FitTrack.Api.Data;
using FitTrack.Api.Domain;
using FitTrack.Api.Features.Feed;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Features.Friends;

public sealed class FriendshipService(AppDbContext db, IFeedNotifier notifier, TimeProvider time)
{
    public async Task<List<FriendDto>> ListFriendsAsync(Guid userId, CancellationToken ct)
    {
        // A friendship is stored once, in the direction it was requested, so look both ways
        var asRequester = await (
                from f in db.Friendships
                where f.Status == FriendshipStatus.Accepted && f.RequesterId == userId
                join u in db.Users on f.AddresseeId equals u.Id
                select new FriendDto(u.Id, u.DisplayName, f.RespondedAt ?? f.CreatedAt))
            .ToListAsync(ct);

        var asAddressee = await (
                from f in db.Friendships
                where f.Status == FriendshipStatus.Accepted && f.AddresseeId == userId
                join u in db.Users on f.RequesterId equals u.Id
                select new FriendDto(u.Id, u.DisplayName, f.RespondedAt ?? f.CreatedAt))
            .ToListAsync(ct);

        return asRequester.Concat(asAddressee).OrderBy(f => f.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<List<Guid>> GetFriendIdsAsync(Guid userId, CancellationToken ct)
    {
        var asRequester = await db.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted && f.RequesterId == userId)
            .Select(f => f.AddresseeId)
            .ToListAsync(ct);

        var asAddressee = await db.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted && f.AddresseeId == userId)
            .Select(f => f.RequesterId)
            .ToListAsync(ct);

        return asRequester.Concat(asAddressee).ToList();
    }

    public Task<bool> AreFriendsAsync(Guid a, Guid b, CancellationToken ct) =>
        db.Friendships.AnyAsync(f => f.Status == FriendshipStatus.Accepted
            && ((f.RequesterId == a && f.AddresseeId == b) || (f.RequesterId == b && f.AddresseeId == a)), ct);

    public async Task<List<FriendRequestDto>> ListRequestsAsync(Guid userId, CancellationToken ct)
    {
        var incoming = await (
                from f in db.Friendships
                where f.Status == FriendshipStatus.Pending && f.AddresseeId == userId
                join u in db.Users on f.RequesterId equals u.Id
                select new FriendRequestDto(f.Id, u.Id, u.DisplayName, "incoming", f.CreatedAt))
            .ToListAsync(ct);

        var outgoing = await (
                from f in db.Friendships
                where f.Status == FriendshipStatus.Pending && f.RequesterId == userId
                join u in db.Users on f.AddresseeId equals u.Id
                select new FriendRequestDto(f.Id, u.Id, u.DisplayName, "outgoing", f.CreatedAt))
            .ToListAsync(ct);

        return incoming.Concat(outgoing).OrderByDescending(r => r.CreatedAt).ToList();
    }

    public async Task<FriendRequestDto> SendRequestAsync(Guid fromId, SendFriendRequest request, CancellationToken ct)
    {
        var email = Validator.Clean(request.Email)?.ToLowerInvariant()
            ?? throw RequestValidationException.For("email", "Required.");

        var target = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct)
            ?? throw new NotFoundException("No member with this e-mail address.");
        if (target.Id == fromId)
        {
            throw RequestValidationException.For("email", "You cannot send a friend request to yourself.");
        }

        var existing = await FindBetweenAsync(fromId, target.Id, ct);
        if (existing is not null)
        {
            throw new ConflictException(existing.Status == FriendshipStatus.Accepted
                ? "You are already friends."
                : existing.RequesterId == fromId
                    ? "You already sent a request to this member."
                    : "This member already sent you a request. Accept it instead.");
        }

        var friendship = new Friendship
        {
            RequesterId = fromId,
            AddresseeId = target.Id,
            Status = FriendshipStatus.Pending,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };
        db.Friendships.Add(friendship);
        await db.SaveChangesAsync(ct);

        var sender = await db.Users.AsNoTracking().SingleAsync(u => u.Id == fromId, ct);
        await notifier.FriendRequestReceivedAsync(target.Id,
            new FriendRequestDto(friendship.Id, sender.Id, sender.DisplayName, "incoming", friendship.CreatedAt), ct);

        return new FriendRequestDto(friendship.Id, target.Id, target.DisplayName, "outgoing", friendship.CreatedAt);
    }

    /// <summary>Only the receiver of a pending request can accept it.</summary>
    public async Task<FriendDto> AcceptAsync(Guid userId, Guid requestId, CancellationToken ct)
    {
        var request = await db.Friendships.SingleOrDefaultAsync(
                f => f.Id == requestId && f.AddresseeId == userId && f.Status == FriendshipStatus.Pending, ct)
            ?? throw new NotFoundException();

        request.Status = FriendshipStatus.Accepted;
        request.RespondedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        var requester = await db.Users.AsNoTracking().SingleAsync(u => u.Id == request.RequesterId, ct);
        var accepter = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId, ct);
        await notifier.FriendRequestAcceptedAsync(requester.Id,
            new FriendDto(accepter.Id, accepter.DisplayName, request.RespondedAt.Value), ct);

        return new FriendDto(requester.Id, requester.DisplayName, request.RespondedAt.Value);
    }

    /// <summary>Declines an incoming request, or cancels an outgoing one.</summary>
    public async Task DeleteRequestAsync(Guid userId, Guid requestId, CancellationToken ct)
    {
        var request = await db.Friendships.SingleOrDefaultAsync(
                f => f.Id == requestId && f.Status == FriendshipStatus.Pending
                     && (f.AddresseeId == userId || f.RequesterId == userId), ct)
            ?? throw new NotFoundException();

        db.Friendships.Remove(request);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveFriendAsync(Guid userId, Guid friendId, CancellationToken ct)
    {
        var friendship = await FindBetweenAsync(userId, friendId, ct);
        if (friendship is not { Status: FriendshipStatus.Accepted })
        {
            throw new NotFoundException();
        }

        db.Friendships.Remove(friendship);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>The profile of yourself or a friend. Anyone else is "not found".</summary>
    public async Task<UserProfileDto> GetProfileAsync(Guid viewerId, Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException();

        if (userId == viewerId)
        {
            return new UserProfileDto(user.Id, user.DisplayName, IsSelf: true, IsFriend: false, FriendsSince: null);
        }

        var friendship = await FindBetweenAsync(viewerId, userId, ct);
        if (friendship is not { Status: FriendshipStatus.Accepted })
        {
            throw new NotFoundException();
        }

        return new UserProfileDto(user.Id, user.DisplayName, IsSelf: false, IsFriend: true,
            FriendsSince: friendship.RespondedAt ?? friendship.CreatedAt);
    }

    private Task<Friendship?> FindBetweenAsync(Guid a, Guid b, CancellationToken ct) =>
        db.Friendships.SingleOrDefaultAsync(
            f => (f.RequesterId == a && f.AddresseeId == b) || (f.RequesterId == b && f.AddresseeId == a), ct);
}
