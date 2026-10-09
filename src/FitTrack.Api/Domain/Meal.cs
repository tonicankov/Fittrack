namespace FitTrack.Api.Domain;

public class Meal : IOwnedEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public required string Name { get; set; }

    public MealType Type { get; set; }

    /// <summary>UTC.</summary>
    public DateTime EatenAt { get; set; }

    public int Calories { get; set; }

    public decimal? ProteinG { get; set; }

    public decimal? CarbsG { get; set; }

    public decimal? FatG { get; set; }

    public string? Notes { get; set; }

    public Visibility Visibility { get; set; }

    public DateTime CreatedAt { get; set; }
}
