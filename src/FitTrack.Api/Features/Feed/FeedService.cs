using FitTrack.Api.Common;
using FitTrack.Api.Data;
using FitTrack.Api.Domain;
using FitTrack.Api.Features.Friends;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Features.Feed;

/// <summary>
/// Stores feed posts. A post refers to the workout or goal it is about and only exists
/// while that entry is visible to friends.
/// </summary>
public sealed class FeedService(
    AppDbContext db,
    FriendshipService friends,
    IFeedNotifier notifier,
    TimeProvider time)
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
        var dto = PostDto.From(post, authorName);
        await notifier.PostCreatedAsync(await AudienceAsync(post.UserId, ct), dto, ct);
        return dto;
    }

    /// <summary>
    /// Keeps the "workout logged" post in step with the workout: created when the workout is
    /// shared with friends, updated when it changes, removed when it becomes private.
    /// </summary>
    public async Task SyncWorkoutPostAsync(Workout workout, CancellationToken ct)
    {
        if (workout.Visibility != Visibility.Friends)
        {
            await RemoveForWorkoutAsync(workout.Id, ct);
            return;
        }

        var summary = PostSummaries.Workout(workout);
        var existing = await db.ActivityPosts.Where(p => p.WorkoutId == workout.Id).ToListAsync(ct);
        if (existing.Count > 0)
        {
            existing.ForEach(p => p.Summary = summary);
            await db.SaveChangesAsync(ct);
            return;
        }

        await PublishAsync(new ActivityPost
        {
            UserId = workout.UserId,
            Type = PostType.WorkoutLogged,
            WorkoutId = workout.Id,
            Summary = summary,
        }, ct);
    }

    public Task<bool> HasWeeklyPostAsync(Guid goalId, DateOnly weekStart, CancellationToken ct) =>
        db.ActivityPosts.AnyAsync(p => p.GoalId == goalId && p.WeekStart == weekStart, ct);

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
        if (posts.Count == 0)
        {
            return posts;
        }

        db.ActivityPosts.RemoveRange(posts);
        await db.SaveChangesAsync(ct);

        foreach (var post in posts)
        {
            await notifier.PostRemovedAsync(await AudienceAsync(post.UserId, ct), post.Id, ct);
        }

        return posts;
    }

    /// <summary>Everyone whose feed shows the author's posts: their friends and themselves.</summary>
    private async Task<List<Guid>> AudienceAsync(Guid authorId, CancellationToken ct)
    {
        var audience = await friends.GetFriendIdsAsync(authorId, ct);
        audience.Add(authorId);
        return audience;
    }
}
