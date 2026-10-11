using FitTrack.Api.Common;
using FitTrack.Api.Domain;

namespace FitTrack.Api.Features.Workouts;

public static class WorkoutValidation
{
    public const int MaxExercises = 50;
    public const int MaxSets = 50;

    public static void Validate(WorkoutRequest request, DateTime now)
    {
        var v = new Validator()
            .Text("name", request.Name, 100)
            .Range("durationMinutes", request.DurationMinutes, 1, 600)
            .Text("notes", request.Notes, 1000, required: false);

        if (request.PerformedAt is { } performedAt
            && (performedAt.UtcDateTime > now.AddDays(1) || performedAt.Year < 2000))
        {
            v.Add("performedAt", "Must be a date in the past.");
        }

        var exercises = request.Exercises ?? [];
        if (exercises.Count == 0)
        {
            v.Add("exercises", "Add at least one exercise.");
        }
        else if (exercises.Count > MaxExercises)
        {
            v.Add("exercises", $"At most {MaxExercises} exercises.");
        }

        for (var i = 0; i < exercises.Count && i < MaxExercises; i++)
        {
            ValidateExercise(v, $"exercises[{i}]", exercises[i]);
        }

        v.ThrowIfInvalid();
    }

    private static void ValidateExercise(Validator v, string path, ExerciseRequest? exercise)
    {
        if (exercise is null)
        {
            v.Add(path, "Required.");
            return;
        }

        v.Text($"{path}.name", exercise.Name, 100);

        switch (exercise.Kind)
        {
            case ExerciseKind.Strength:
                var sets = exercise.Sets ?? [];
                if (sets.Count == 0)
                {
                    v.Add($"{path}.sets", "Add at least one set.");
                }
                else if (sets.Count > MaxSets)
                {
                    v.Add($"{path}.sets", $"At most {MaxSets} sets.");
                }

                for (var j = 0; j < sets.Count && j < MaxSets; j++)
                {
                    v.Range($"{path}.sets[{j}].reps", sets[j]?.Reps, 1, 1000)
                     .Range($"{path}.sets[{j}].weightKg", sets[j]?.WeightKg, 0, 1000, required: false);
                }
                break;

            case ExerciseKind.Cardio:
                if (exercise.DurationSeconds is null && exercise.DistanceKm is null)
                {
                    v.Add($"{path}.durationSeconds", "Enter a duration, a distance or both.");
                }
                v.Range($"{path}.durationSeconds", exercise.DurationSeconds, 1, 86_400, required: false)
                 .Range($"{path}.distanceKm", exercise.DistanceKm, 0, 1000, required: false);
                break;

            default:
                v.Add($"{path}.kind", "Required.");
                break;
        }
    }
}
