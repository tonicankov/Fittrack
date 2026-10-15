using FitTrack.Api.Common;
using FitTrack.Api.Data;
using FitTrack.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Features.Feed;

/// <summary>
/// Stores feed posts. A post refers to the workout or goal it is about and only exists
/// while that entry is visible to friends.
/// </summary>
public sealed class FeedService(AppDbContext db, TimeProvider time)
{
    public const int PageSize = 20;

    public async Task<PostDto> PublishAsync(ActivityPost post, CancellationToken ct)
    {
        if (post.CreatedAt == default)
        {
            post.CreatedAt = time.GetUtcNow().UtcDateTime;
        }

        db.ActivityPosts.Add(post);
        await db.SaveChangesAsync(ct);

        var authorName = await db.Users.Where(u => u.Id == post.UserId).Select(u => u.DisplayName).SingleAsync(ct);
        return PostDto.From(post, authorName);
    }

    public Task RemoveForWorkoutAsync(Guid workoutId, CancellationToken ct) =>
        RemoveAsync(db.ActivityPosts.Where(p => p.WorkoutId == workoutId), ct);

    public Task RemoveForGoalAsync(Guid goalId, CancellationToken ct) =>
        RemoveAsync(db.ActivityPosts.Where(p => p.GoalId == goalId), ct);

    /// <summary>Posts by the member and their friends, newest first.</summary>
    public async Task<Page<PostDto>> GetFeedAsync(Guid userId, DateTime? before, CancellationToken ct)
    {
        var posts = db.ActivityPosts.AsNoTracking().Where(p => p.UserId == userId
            || db.Friendships.Any(f => f.Status == FriendshipStatus.Accepted
                && ((f.RequesterId == userId && f.AddresseeId == p.UserId)
                    || (f.AddresseeId == userId && f.RequesterId == p.UserId))));

        if (before is not null)
        {
            posts = posts.Where(p => p.CreatedAt < before);
        }

        var rows = await (
                from p in posts
                join u in db.Users on p.UserId equals u.Id
                orderby p.CreatedAt descending
                select new { Post = p, AuthorName = u.DisplayName })
            .Take(PageSize + 1)
            .ToListAsync(ct);

        return Page<PostDto>.From(rows, PageSize, r => r.Post.CreatedAt, r => PostDto.From(r.Post, r.AuthorName));
    }

    private async Task<List<ActivityPost>> RemoveAsync(IQueryable<ActivityPost> query, CancellationToken ct)
    {
        var posts = await query.ToListAsync(ct);
        if (posts.Count > 0)
        {
            db.ActivityPosts.RemoveRange(posts);
            await db.SaveChangesAsync(ct);
        }

        return posts;
    }
}
