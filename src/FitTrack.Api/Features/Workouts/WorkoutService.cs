using FitTrack.Api.Common;
using FitTrack.Api.Data;
using FitTrack.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Features.Workouts;

public sealed class WorkoutService(AppDbContext db, MemberSettingsProvider members, TimeProvider time)
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

    public async Task<WorkoutDto> GetAsync(Guid viewerId, Guid id, CancellationToken ct)
    {
        var workout = await WithExercises(db.Workouts.AsNoTracking())
            .SingleOrDefaultAsync(w => w.Id == id && w.UserId == viewerId, ct)
            ?? throw new NotFoundException();
        return WorkoutDto.From(workout);
    }

    public async Task<Page<WorkoutDto>> ListAsync(Guid viewerId, Guid ownerId, DateTime? before, CancellationToken ct)
    {
        if (viewerId != ownerId)
        {
            throw new NotFoundException();
        }

        var query = WithExercises(db.Workouts.AsNoTracking()).Where(w => w.UserId == ownerId);
        if (before is not null)
        {
            query = query.Where(w => w.PerformedAt < before);
        }

        var rows = await query.OrderByDescending(w => w.PerformedAt).Take(PageSize + 1).ToListAsync(ct);
        return Page<WorkoutDto>.From(rows, PageSize, w => w.PerformedAt, WorkoutDto.From);
    }

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
