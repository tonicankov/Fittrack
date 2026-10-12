using FitTrack.Api.Common;
using FitTrack.Api.Data;
using FitTrack.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Features.Meals;

public sealed class MealService(AppDbContext db, MemberSettingsProvider members, TimeProvider time)
{
    public async Task<MealDto> CreateAsync(Guid userId, MealRequest request, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        Validate(request, now);
        var settings = await members.GetAsync(userId, ct);

        var meal = new Meal { UserId = userId, Name = "", CreatedAt = now };
        Apply(meal, request, request.Visibility ?? settings.DefaultVisibility, now);
        db.Meals.Add(meal);
        await db.SaveChangesAsync(ct);

        return MealDto.From(meal);
    }

    public async Task<MealDto> UpdateAsync(Guid userId, Guid id, MealRequest request, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        Validate(request, now);

        var meal = await LoadOwnAsync(userId, id, ct);
        Apply(meal, request, request.Visibility ?? meal.Visibility, now);
        await db.SaveChangesAsync(ct);

        return MealDto.From(meal);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken ct)
    {
        db.Meals.Remove(await LoadOwnAsync(userId, id, ct));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>The meals of one calendar day in the owner's time zone (default: today).</summary>
    public async Task<List<MealDto>> ListAsync(Guid viewerId, Guid ownerId, DateOnly? date, CancellationToken ct)
    {
        if (viewerId != ownerId)
        {
            throw new NotFoundException();
        }

        var tz = (await members.GetAsync(ownerId, ct)).TimeZone;
        var day = date ?? TimeZones.Today(time, tz);
        var meals = await MealsBetween(db.Meals.AsNoTracking().Where(m => m.UserId == ownerId), day, day, tz)
            .OrderBy(m => m.EatenAt)
            .ToListAsync(ct);

        return meals.Select(MealDto.From).ToList();
    }

    private async Task<Meal> LoadOwnAsync(Guid userId, Guid id, CancellationToken ct) =>
        await db.Meals.SingleOrDefaultAsync(m => m.Id == id && m.UserId == userId, ct) ?? throw new NotFoundException();

    /// <summary>Meals eaten from the start of <paramref name="from"/> up to the end of <paramref name="to"/>, local time.</summary>
    public static IQueryable<Meal> MealsBetween(IQueryable<Meal> meals, DateOnly from, DateOnly to, TimeZoneInfo tz)
    {
        var start = TimeZones.StartOfDayUtc(from, tz);
        var end = TimeZones.StartOfDayUtc(to.AddDays(1), tz);
        return meals.Where(m => m.EatenAt >= start && m.EatenAt < end);
    }

    private static void Validate(MealRequest request, DateTime now)
    {
        var v = new Validator()
            .Text("name", request.Name, 100)
            .Range("calories", request.Calories, 0, 10_000)
            .Range("proteinG", request.ProteinG, 0, 1000, required: false)
            .Range("carbsG", request.CarbsG, 0, 1000, required: false)
            .Range("fatG", request.FatG, 0, 1000, required: false)
            .Text("notes", request.Notes, 1000, required: false);

        if (request.Type is null)
        {
            v.Add("type", "Required.");
        }

        if (request.EatenAt is { } eatenAt && (eatenAt.UtcDateTime > now.AddDays(1) || eatenAt.Year < 2000))
        {
            v.Add("eatenAt", "Must be a date in the past.");
        }

        v.ThrowIfInvalid();
    }

    private static void Apply(Meal meal, MealRequest request, Visibility visibility, DateTime now)
    {
        meal.Name = request.Name!.Trim();
        meal.Type = request.Type!.Value;
        meal.EatenAt = request.EatenAt?.UtcDateTime ?? now;
        meal.Calories = request.Calories!.Value;
        meal.ProteinG = request.ProteinG;
        meal.CarbsG = request.CarbsG;
        meal.FatG = request.FatG;
        meal.Notes = Validator.Clean(request.Notes);
        meal.Visibility = visibility;
    }
}
