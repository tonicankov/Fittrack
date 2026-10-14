using FitTrack.Api.Domain;

namespace FitTrack.Api.Features.Goals;

public sealed record CreateGoalRequest(GoalMetric? Metric, decimal? TargetValue, DateOnly? TargetDate, Visibility? Visibility);

/// <summary>Status can be set to Active or Archived; Achieved is only set by the system.</summary>
public sealed record UpdateGoalRequest(decimal? TargetValue, DateOnly? TargetDate, Visibility? Visibility, GoalStatus? Status);

public sealed record GoalDto(
    Guid Id,
    Guid UserId,
    GoalMetric Metric,
    decimal TargetValue,
    decimal? StartValue,
    DateOnly StartDate,
    DateOnly? TargetDate,
    GoalStatus Status,
    DateTime? AchievedAt,
    Visibility Visibility,
    GoalProgressDto? Progress);

/// <param name="Current">Latest weight, workouts this week, or today's calories/protein. Null when nothing is logged.</param>
/// <param name="Percent">0–100, for a progress bar.</param>
/// <param name="IsMet">Whether the target is reached for the current period.</param>
/// <param name="Period">"latest", "thisWeek" or "today".</param>
public sealed record GoalProgressDto(decimal? Current, decimal Percent, bool IsMet, string Period);
