using FitTrack.Api.Common;
using FitTrack.Api.Data;
using FitTrack.Api.Domain;
using FitTrack.Api.Features.Friends;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Features.Workouts;

public sealed class WorkoutService(
    AppDbContext db,
    MemberSettingsProvider members,
    VisibilityService visibility,
    TimeProvider time)
{
    public const int PageSize = 20;

    public async Task<WorkoutDto> CreateAsync(Guid userId, WorkoutRequest request, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        WorkoutValidation.Validate(request, now);
        var settings = await members.GetAsync(userId, ct);

        var workout = new Workout { UserId = userId, Name = "", CreatedAt = now };
        Apply(workout, request, request.Visibility ?? settings.DefaultVisibility, now);
        db.Workouts.Add(workout);
        await db.SaveChangesAsync(ct);

        return WorkoutDto.From(workout);
    }

    public async Task<WorkoutDto> UpdateAsync(Guid userId, Guid id, WorkoutRequest request, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        WorkoutValidation.Validate(request, now);

        var workout = await LoadOwnAsync(userId, id, ct);
        // Exercises and sets are replaced as a whole; that is simpler than diffing them
        db.WorkoutExercises.RemoveRange(workout.Exercises);
        Apply(workout, request, request.Visibility ?? workout.Visibility, now);
        // The new rows already have their Guid keys, so add them explicitly;
        // otherwise EF would take them for existing rows and try to update them.
        db.WorkoutExercises.AddRange(workout.Exercises);
        await db.SaveChangesAsync(ct);

        return WorkoutDto.From(workout);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var workout = await LoadOwnAsync(userId, id, ct);
        db.Workouts.Remove(workout);
        await db.SaveChangesAsync(ct);
    }

    public async Task<WorkoutDto> GetAsync(Guid viewerId, Guid id, CancellationToken ct)
    {
        var workout = await WithExercises(visibility.VisibleTo(db.Workouts.AsNoTracking(), viewerId))
            .SingleOrDefaultAsync(w => w.Id == id, ct)
            ?? throw new NotFoundException();
        return WorkoutDto.From(workout);
    }

    public async Task<Page<WorkoutDto>> ListAsync(Guid viewerId, Guid ownerId, DateTime? before, CancellationToken ct)
    {
        var query = WithExercises(visibility.VisibleTo(db.Workouts.AsNoTracking(), viewerId))
            .Where(w => w.UserId == ownerId);
        if (before is not null)
        {
            query = query.Where(w => w.PerformedAt < before);
        }

        var rows = await query.OrderByDescending(w => w.PerformedAt).Take(PageSize + 1).ToListAsync(ct);
        return Page<WorkoutDto>.From(rows, PageSize, w => w.PerformedAt, WorkoutDto.From);
    }

    /// <summary>Names of exercises the member logged before, so the same exercise is always spelled the same way.</summary>
    public async Task<List<string>> SuggestExerciseNamesAsync(Guid userId, string? prefix, CancellationToken ct)
    {
        // Lower-case both sides so the match does not depend on the column collation
        var start = (Validator.Clean(prefix) ?? "").ToLowerInvariant();

        return await (
                from x in db.WorkoutExercises
                join w in db.Workouts on x.WorkoutId equals w.Id
                where w.UserId == userId && x.Name.ToLower().StartsWith(start)
                select x.Name)
            .Distinct()
            .OrderBy(name => name)
            .Take(10)
            .ToListAsync(ct);
    }

    private async Task<Workout> LoadOwnAsync(Guid userId, Guid id, CancellationToken ct) =>
        await WithExercises(db.Workouts).SingleOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct)
        ?? throw new NotFoundException();

    private static IQueryable<Workout> WithExercises(IQueryable<Workout> query) =>
        query.Include(w => w.Exercises).ThenInclude(x => x.Sets);

    private static void Apply(Workout workout, WorkoutRequest request, Visibility visibility, DateTime now)
    {
        workout.Name = request.Name!.Trim();
        workout.PerformedAt = request.PerformedAt?.UtcDateTime ?? now;
        workout.DurationMinutes = request.DurationMinutes!.Value;
        workout.Notes = Validator.Clean(request.Notes);
        workout.Visibility = visibility;
        workout.Exercises = request.Exercises!.Select((x, i) => new WorkoutExercise
        {
            Position = i,
            Name = x.Name!.Trim(),
            Kind = x.Kind!.Value,
            DurationSeconds = x.Kind == ExerciseKind.Cardio ? x.DurationSeconds : null,
            DistanceKm = x.Kind == ExerciseKind.Cardio ? x.DistanceKm : null,
            Sets = x.Kind == ExerciseKind.Strength
                ? (x.Sets ?? []).Select((s, j) => new ExerciseSet
                {
                    Position = j,
                    Reps = s.Reps!.Value,
                    WeightKg = s.WeightKg ?? 0,
                }).ToList()
                : [],
        }).ToList();
    }
}
