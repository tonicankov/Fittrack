using FitTrack.Api.Domain;

namespace FitTrack.Api.Features.Meals;

public sealed record MealRequest(
    string? Name,
    MealType? Type,
    DateTimeOffset? EatenAt,
    int? Calories,
    decimal? ProteinG,
    decimal? CarbsG,
    decimal? FatG,
    string? Notes,
    Visibility? Visibility);

public sealed record MealDto(
    Guid Id,
    Guid UserId,
    string Name,
    MealType Type,
    DateTime EatenAt,
    int Calories,
    decimal? ProteinG,
    decimal? CarbsG,
    decimal? FatG,
    string? Notes,
    Visibility Visibility,
    DateTime CreatedAt)
{
    public static MealDto From(Meal m) => new(
        m.Id, m.UserId, m.Name, m.Type, m.EatenAt, m.Calories, m.ProteinG, m.CarbsG, m.FatG, m.Notes, m.Visibility, m.CreatedAt);
}

public sealed record DailyTotalsDto(
    DateOnly Date,
    int MealCount,
    int Calories,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG);
