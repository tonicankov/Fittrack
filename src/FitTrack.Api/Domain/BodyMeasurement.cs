namespace FitTrack.Api.Domain;

/// <summary>At most one per member per calendar day.</summary>
public class BodyMeasurement : IOwnedEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    /// <summary>Calendar date in the member's own time zone.</summary>
    public DateOnly Date { get; set; }

    public decimal WeightKg { get; set; }

    public decimal? BodyFatPct { get; set; }

    public decimal? WaistCm { get; set; }

    public Visibility Visibility { get; set; }

    public DateTime CreatedAt { get; set; }
}
