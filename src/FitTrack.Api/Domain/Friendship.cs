namespace FitTrack.Api.Domain;

/// <summary>
/// One row per pair of members, stored in the direction the request was sent.
/// Once accepted, the two members are friends in both directions.
/// </summary>
public class Friendship
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RequesterId { get; set; }

    public Guid AddresseeId { get; set; }

    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    public DateTime CreatedAt { get; set; }

    public DateTime? RespondedAt { get; set; }
}
