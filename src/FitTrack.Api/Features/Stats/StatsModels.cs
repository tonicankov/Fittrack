namespace FitTrack.Api.Features.Stats;

public sealed record WeightPoint(DateOnly Date, decimal WeightKg);

public sealed record WeightTrendDto(List<WeightPoint> Points, decimal? TargetKg);

public sealed record WeekCountDto(DateOnly WeekStart, int Workouts);

public sealed record WorkoutsPerWeekDto(List<WeekCountDto> Weeks, decimal? Target);

public sealed record WeekVolumeDto(DateOnly WeekStart, decimal VolumeKg);

/// <summary>Calories and protein are null on days without any logged meal.</summary>
public sealed record NutritionDayDto(DateOnly Date, int? Calories, decimal? ProteinG);

public sealed record NutritionDto(List<NutritionDayDto> Days, decimal? CaloriesTarget, decimal? ProteinTarget);
