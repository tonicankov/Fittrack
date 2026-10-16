using System.Globalization;
using FitTrack.Api.Domain;

namespace FitTrack.Api.Features.Feed;

/// <summary>The one-line texts shown in the feed. Written when the post is created.</summary>
public static class PostSummaries
{
    public static string Workout(Workout workout)
    {
        var count = workout.Exercises.Count;
        return Limit($"{workout.Name}: {count} exercise{(count == 1 ? "" : "s")}, {workout.DurationMinutes} min");
    }

    public static string WeightGoalReached(decimal targetKg) =>
        string.Create(CultureInfo.InvariantCulture, $"Reached the target weight of {targetKg:0.#} kg");

    public static string WeeklyTargetMet(decimal workoutsPerWeek) =>
        string.Create(CultureInfo.InvariantCulture, $"Hit the weekly goal of {workoutsPerWeek:0} workouts");

    private static string Limit(string text) => text.Length <= 200 ? text : text[..199] + "…";
}
