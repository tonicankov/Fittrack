namespace FitTrack.Api.Domain;

public class Goal : IOwnedEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public GoalMetric Metric { get; set; }

    /// <summary>kg, workouts, kcal or grams depending on <see cref="Metric"/>.</summary>
    public decimal TargetValue { get; set; }

    /// <summary>Body weight only: the latest weight when the goal was created. Decides lose vs. gain.</summary>
    public decimal? StartValue { get; set; }

    public DateOnly StartDate { get; set; }

    /// <summary>Body weight only.</summary>
    public DateOnly? TargetDate { get; set; }

    public GoalStatus Status { get; set; } = GoalStatus.Active;

    public DateTime? AchievedAt { get; set; }

    public Visibility Visibility { get; set; }

    public DateTime CreatedAt { get; set; }
}
