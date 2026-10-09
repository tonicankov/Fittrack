namespace FitTrack.Api.Domain;

public enum Visibility
{
    Private,
    Friends,
}

public enum FriendshipStatus
{
    Pending,
    Accepted,
}

public enum ExerciseKind
{
    Strength,
    Cardio,
}

public enum MealType
{
    Breakfast,
    Lunch,
    Dinner,
    Snack,
}

public enum GoalMetric
{
    BodyWeight,
    WorkoutsPerWeek,
    DailyCalories,
    DailyProtein,
}

public enum GoalStatus
{
    Active,
    Achieved,
    Archived,
}

public enum PostType
{
    WorkoutLogged,
    GoalAchieved,
    WeeklyTargetMet,
}
