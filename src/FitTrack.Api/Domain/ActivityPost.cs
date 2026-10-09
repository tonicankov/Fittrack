namespace FitTrack.Api.Domain;

/// <summary>
/// A feed entry. It only exists while the workout or goal it refers to is visible to friends.
/// </summary>
public class ActivityPost
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The author.</summary>
    public Guid UserId { get; set; }

    public PostType Type { get; set; }

    public Guid? WorkoutId { get; set; }

    public Guid? GoalId { get; set; }

    /// <summary>Weekly target posts only, so each week is posted at most once.</summary>
    public DateOnly? WeekStart { get; set; }

    public required string Summary { get; set; }

    public DateTime CreatedAt { get; set; }
}
