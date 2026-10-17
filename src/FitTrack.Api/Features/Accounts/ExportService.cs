using FitTrack.Api.Data;
using FitTrack.Api.Features.Friends;
using FitTrack.Api.Features.Goals;
using FitTrack.Api.Features.Meals;
using FitTrack.Api.Features.Measurements;
using FitTrack.Api.Features.Workouts;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Features.Accounts;

public sealed record ExportDto(
    DateTime ExportedAt,
    MeDto Profile,
    List<WorkoutDto> Workouts,
    List<MealDto> Meals,
    List<MeasurementDto> Measurements,
    List<GoalDto> Goals,
    List<FriendDto> Friends);

/// <summary>Everything the system stores about a member, in one document (GDPR right of access).</summary>
public sealed class ExportService(
    AppDbContext db,
    AccountService accounts,
    GoalService goals,
    FriendshipService friends,
    TimeProvider time)
{
    public async Task<ExportDto> ExportAsync(Guid userId, CancellationToken ct)
    {
        var workouts = await db.Workouts.AsNoTracking()
            .Include(w => w.Exercises).ThenInclude(x => x.Sets)
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.PerformedAt)
            .ToListAsync(ct);

        var meals = await db.Meals.AsNoTracking().Where(m => m.UserId == userId).OrderBy(m => m.EatenAt).ToListAsync(ct);
        var measurements = await db.BodyMeasurements.AsNoTracking().Where(m => m.UserId == userId).OrderBy(m => m.Date).ToListAsync(ct);

        return new ExportDto(
            time.GetUtcNow().UtcDateTime,
            await accounts.GetMeAsync(userId, ct),
            workouts.Select(WorkoutDto.From).ToList(),
            meals.Select(MealDto.From).ToList(),
            measurements.Select(MeasurementDto.From).ToList(),
            await goals.ListAsync(userId, userId, ct),
            await friends.ListFriendsAsync(userId, ct));
    }
}
