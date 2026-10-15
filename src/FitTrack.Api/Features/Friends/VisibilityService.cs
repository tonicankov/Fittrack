using FitTrack.Api.Data;
using FitTrack.Api.Domain;

namespace FitTrack.Api.Features.Friends;

/// <summary>
/// The privacy rule, in one place: a member sees their own entries, plus their friends'
/// entries that have visibility Friends. Everything else behaves as if it did not exist.
/// </summary>
public sealed class VisibilityService(AppDbContext db)
{
    public IQueryable<T> VisibleTo<T>(IQueryable<T> query, Guid viewerId) where T : class, IOwnedEntry =>
        query.Where(e => e.UserId == viewerId
            || (e.Visibility == Visibility.Friends
                && db.Friendships.Any(f => f.Status == FriendshipStatus.Accepted
                    && ((f.RequesterId == viewerId && f.AddresseeId == e.UserId)
                        || (f.AddresseeId == viewerId && f.RequesterId == e.UserId)))));
}
