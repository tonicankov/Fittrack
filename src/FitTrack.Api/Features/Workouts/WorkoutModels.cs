using FitTrack.Api.Domain;

namespace FitTrack.Api.Features.Workouts;

public sealed record WorkoutRequest(
    string? Name,
    DateTimeOffset? PerformedAt,
    int? DurationMinutes,
    string? Notes,
    Visibility? Visibility,
    List<ExerciseRequest>? Exercises);

public sealed record ExerciseRequest(
    string? Name,
    ExerciseKind? Kind,
    int? DurationSeconds,
    decimal? DistanceKm,
    List<SetRequest>? Sets);

public sealed record SetRequest(int? Reps, decimal? WeightKg);

public sealed record WorkoutDto(
    Guid Id,
    Guid UserId,
    string Name,
    DateTime PerformedAt,
    int DurationMinutes,
    string? Notes,
    Visibility Visibility,
    DateTime CreatedAt,
    List<ExerciseDto> Exercises)
{
    public static WorkoutDto From(Workout w) => new(
        w.Id, w.UserId, w.Name, w.PerformedAt, w.DurationMinutes, w.Notes, w.Visibility, w.CreatedAt,
        w.Exercises.OrderBy(x => x.Position).Select(ExerciseDto.From).ToList());
}

public sealed record ExerciseDto(
    Guid Id,
    int Position,
    string Name,
    ExerciseKind Kind,
    int? DurationSeconds,
    decimal? DistanceKm,
    List<SetDto> Sets)
{
    public static ExerciseDto From(WorkoutExercise x) => new(
        x.Id, x.Position, x.Name, x.Kind, x.DurationSeconds, x.DistanceKm,
        x.Sets.OrderBy(s => s.Position).Select(s => new SetDto(s.Position, s.Reps, s.WeightKg)).ToList());
}

public sealed record SetDto(int Position, int Reps, decimal WeightKg);
