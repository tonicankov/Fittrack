using FitTrack.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Tests;

/// <summary>
/// Checks that the rules declared in AppDbContext actually hold in MySQL.
/// </summary>
[Collection(ApiCollection.Name)]
public class DataModelTests(ApiFactory factory)
{
    private static User NewUser() => new()
    {
        Email = $"{Guid.NewGuid():N}@example.com",
        DisplayName = "Test user",
        PasswordHash = "not-a-real-hash",
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task Workout_with_exercises_and_sets_round_trips()
    {
        var user = NewUser();
        var workout = new Workout
        {
            UserId = user.Id,
            Name = "Leg day",
            PerformedAt = DateTime.UtcNow,
            DurationMinutes = 55,
            Visibility = Visibility.Friends,
            Exercises =
            [
                new()
                {
                    Name = "Squat", Position = 0, Kind = ExerciseKind.Strength,
                    Sets = [new() { Position = 0, Reps = 5, WeightKg = 80m }, new() { Position = 1, Reps = 5, WeightKg = 82.5m }],
                },
                new() { Name = "Bike", Position = 1, Kind = ExerciseKind.Cardio, DurationSeconds = 600, DistanceKm = 4.2m },
            ],
        };

        await using (var db = factory.CreateDbContext())
        {
            db.Users.Add(user);
            db.Workouts.Add(workout);
            await db.SaveChangesAsync();
        }

        await using (var db = factory.CreateDbContext())
        {
            var loaded = await db.Workouts
                .Include(w => w.Exercises).ThenInclude(x => x.Sets)
                .SingleAsync(w => w.Id == workout.Id);

            Assert.Equal(2, loaded.Exercises.Count);
            var squat = loaded.Exercises.Single(x => x.Name == "Squat");
            Assert.Equal(new[] { 80m, 82.5m }, squat.Sets.OrderBy(s => s.Position).Select(s => s.WeightKg));
            Assert.Equal(ExerciseKind.Cardio, loaded.Exercises.Single(x => x.Name == "Bike").Kind);
            Assert.Equal(DateTimeKind.Utc, loaded.PerformedAt.Kind);
        }
    }

    [Fact]
    public async Task Only_one_body_measurement_per_day()
    {
        var user = NewUser();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await using var db = factory.CreateDbContext();
        db.Users.Add(user);
        db.BodyMeasurements.Add(new BodyMeasurement { UserId = user.Id, Date = today, WeightKg = 80m });
        await db.SaveChangesAsync();

        db.BodyMeasurements.Add(new BodyMeasurement { UserId = user.Id, Date = today, WeightKg = 79.5m });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Email_must_be_unique()
    {
        var first = NewUser();
        var duplicate = NewUser();
        duplicate.Email = first.Email;

        await using var db = factory.CreateDbContext();
        db.Users.Add(first);
        await db.SaveChangesAsync();

        db.Users.Add(duplicate);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Deleting_a_user_removes_their_data()
    {
        var user = NewUser();
        var meal = new Meal { UserId = user.Id, Name = "Oats", Type = MealType.Breakfast, EatenAt = DateTime.UtcNow, Calories = 350 };
        var goal = new Goal { UserId = user.Id, Metric = GoalMetric.DailyProtein, TargetValue = 140, StartDate = new DateOnly(2026, 1, 1) };

        await using (var db = factory.CreateDbContext())
        {
            db.Users.Add(user);
            db.Meals.Add(meal);
            db.Goals.Add(goal);
            await db.SaveChangesAsync();
        }

        await using (var db = factory.CreateDbContext())
        {
            db.Users.Remove(await db.Users.SingleAsync(u => u.Id == user.Id));
            await db.SaveChangesAsync();
        }

        await using (var db = factory.CreateDbContext())
        {
            Assert.False(await db.Meals.AnyAsync(m => m.UserId == user.Id));
            Assert.False(await db.Goals.AnyAsync(g => g.UserId == user.Id));
        }
    }
}
