namespace FitTrack.Api.Domain;

public class Workout : IOwnedEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public required string Name { get; set; }

    /// <summary>Start time in UTC.</summary>
    public DateTime PerformedAt { get; set; }

    public int DurationMinutes { get; set; }

    public string? Notes { get; set; }

    public Visibility Visibility { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<WorkoutExercise> Exercises { get; set; } = [];
}

public class WorkoutExercise
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkoutId { get; set; }

    /// <summary>Order within the workout, starting at 0.</summary>
    public int Position { get; set; }

    public required string Name { get; set; }

    public ExerciseKind Kind { get; set; }

    /// <summary>Cardio only.</summary>
    public int? DurationSeconds { get; set; }

    /// <summary>Cardio only.</summary>
    public decimal? DistanceKm { get; set; }

    /// <summary>Strength only: one row per set.</summary>
    public List<ExerciseSet> Sets { get; set; } = [];
}

public class ExerciseSet
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkoutExerciseId { get; set; }

    public int Position { get; set; }

    public int Reps { get; set; }

    /// <summary>0 means body weight only.</summary>
    public decimal WeightKg { get; set; }
}
