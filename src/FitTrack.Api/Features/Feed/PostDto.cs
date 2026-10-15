using FitTrack.Api.Domain;

namespace FitTrack.Api.Features.Feed;

public sealed record PostDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    PostType Type,
    string Summary,
    Guid? WorkoutId,
    Guid? GoalId,
    DateTime CreatedAt)
{
    public static PostDto From(ActivityPost p, string authorName) =>
        new(p.Id, p.UserId, authorName, p.Type, p.Summary, p.WorkoutId, p.GoalId, p.CreatedAt);
}
