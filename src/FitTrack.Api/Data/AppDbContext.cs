using FitTrack.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Invite> Invites => Set<Invite>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
    public DbSet<ExerciseSet> ExerciseSets => Set<ExerciseSet>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<ActivityPost> ActivityPosts => Set<ActivityPost>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enums as readable strings instead of numbers
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(32);

        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();

        configurationBuilder.Properties<DateOnly>().HaveConversion<DateOnlyConverter>().HaveColumnType("date");
        configurationBuilder.Properties<DateOnly?>().HaveConversion<DateOnlyConverter>().HaveColumnType("date");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Email).HasMaxLength(256);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.DisplayName).HasMaxLength(50);
            e.Property(u => u.PasswordHash).HasMaxLength(256);
            e.Property(u => u.TimeZone).HasMaxLength(64);
        });

        modelBuilder.Entity<Invite>(e =>
        {
            e.Property(i => i.Code).HasMaxLength(12);
            e.HasIndex(i => i.Code).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(i => i.CreatedById).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(i => i.UsedById).OnDelete(DeleteBehavior.SetNull);
            // Two people redeeming the same code at once: only the first update wins
            e.Property(i => i.UsedById).IsConcurrencyToken();
        });

        modelBuilder.Entity<Friendship>(e =>
        {
            e.HasIndex(f => new { f.RequesterId, f.AddresseeId }).IsUnique();
            e.HasIndex(f => f.AddresseeId);
            e.HasOne<User>().WithMany().HasForeignKey(f => f.RequesterId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(f => f.AddresseeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Workout>(e =>
        {
            e.Property(w => w.Name).HasMaxLength(100);
            e.Property(w => w.Notes).HasMaxLength(1000);
            e.HasIndex(w => new { w.UserId, w.PerformedAt });
            e.HasOne<User>().WithMany().HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(w => w.Exercises).WithOne().HasForeignKey(x => x.WorkoutId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutExercise>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.DistanceKm).HasPrecision(6, 2);
            e.HasIndex(x => new { x.WorkoutId, x.Position });
            e.HasMany(x => x.Sets).WithOne().HasForeignKey(s => s.WorkoutExerciseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExerciseSet>(e =>
        {
            e.Property(s => s.WeightKg).HasPrecision(6, 2);
        });

        modelBuilder.Entity<Meal>(e =>
        {
            e.Property(m => m.Name).HasMaxLength(100);
            e.Property(m => m.Notes).HasMaxLength(1000);
            e.Property(m => m.ProteinG).HasPrecision(6, 1);
            e.Property(m => m.CarbsG).HasPrecision(6, 1);
            e.Property(m => m.FatG).HasPrecision(6, 1);
            e.HasIndex(m => new { m.UserId, m.EatenAt });
            e.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BodyMeasurement>(e =>
        {
            e.Property(m => m.WeightKg).HasPrecision(5, 2);
            e.Property(m => m.BodyFatPct).HasPrecision(4, 1);
            e.Property(m => m.WaistCm).HasPrecision(5, 1);
            e.HasIndex(m => new { m.UserId, m.Date }).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Goal>(e =>
        {
            e.Property(g => g.TargetValue).HasPrecision(8, 2);
            e.Property(g => g.StartValue).HasPrecision(8, 2);
            e.HasIndex(g => new { g.UserId, g.Status });
            e.HasOne<User>().WithMany().HasForeignKey(g => g.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActivityPost>(e =>
        {
            e.Property(p => p.Summary).HasMaxLength(200);
            e.HasIndex(p => new { p.UserId, p.CreatedAt });
            e.HasIndex(p => new { p.GoalId, p.WeekStart }).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Workout>().WithMany().HasForeignKey(p => p.WorkoutId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Goal>().WithMany().HasForeignKey(p => p.GoalId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
