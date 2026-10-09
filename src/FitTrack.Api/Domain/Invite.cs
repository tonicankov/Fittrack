namespace FitTrack.Api.Domain;

public class Invite
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Code { get; set; }

    /// <summary>Null for the bootstrap invite that creates the very first account.</summary>
    public Guid? CreatedById { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public Guid? UsedById { get; set; }

    public DateTime? UsedAt { get; set; }
}
