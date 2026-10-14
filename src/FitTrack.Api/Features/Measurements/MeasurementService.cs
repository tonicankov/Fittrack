using FitTrack.Api.Common;
using FitTrack.Api.Data;
using FitTrack.Api.Domain;
using FitTrack.Api.Features.Goals;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Features.Measurements;

public sealed class MeasurementService(AppDbContext db, MemberSettingsProvider members, GoalService goals, TimeProvider time)
{
    /// <summary>
    /// Records the measurement of a day. There is at most one per day, so recording
    /// again replaces the earlier one; retrying the same request is harmless.
    /// </summary>
    public async Task<MeasurementDto> UpsertAsync(Guid userId, DateOnly date, MeasurementRequest request, CancellationToken ct)
    {
        var settings = await members.GetAsync(userId, ct);
        var today = TimeZones.Today(time, settings.TimeZone);

        var v = new Validator()
            .Range("weightKg", request.WeightKg, 20, 400)
            .Range("bodyFatPct", request.BodyFatPct, 2, 75, required: false)
            .Range("waistCm", request.WaistCm, 30, 250, required: false);
        if (date > today.AddDays(1) || date.Year < 2000)
        {
            v.Add("date", "Must be a date in the past.");
        }
        v.ThrowIfInvalid();

        var measurement = await db.BodyMeasurements.SingleOrDefaultAsync(m => m.UserId == userId && m.Date == date, ct);
        if (measurement is null)
        {
            measurement = new BodyMeasurement
            {
                UserId = userId,
                Date = date,
                Visibility = settings.DefaultVisibility,
                CreatedAt = time.GetUtcNow().UtcDateTime,
            };
            db.BodyMeasurements.Add(measurement);
        }

        measurement.WeightKg = request.WeightKg!.Value;
        measurement.BodyFatPct = request.BodyFatPct;
        measurement.WaistCm = request.WaistCm;
        measurement.Visibility = request.Visibility ?? measurement.Visibility;
        await db.SaveChangesAsync(ct);

        await goals.EvaluateAsync(userId, ct);

        return MeasurementDto.From(measurement);
    }

    public async Task DeleteAsync(Guid userId, DateOnly date, CancellationToken ct)
    {
        var measurement = await db.BodyMeasurements.SingleOrDefaultAsync(m => m.UserId == userId && m.Date == date, ct)
            ?? throw new NotFoundException();
        db.BodyMeasurements.Remove(measurement);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Measurements newest first, optionally limited to a date range.</summary>
    public async Task<List<MeasurementDto>> ListAsync(Guid viewerId, Guid ownerId, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        if (viewerId != ownerId)
        {
            throw new NotFoundException();
        }

        var query = db.BodyMeasurements.AsNoTracking().Where(m => m.UserId == ownerId);
        if (from is not null)
        {
            query = query.Where(m => m.Date >= from);
        }
        if (to is not null)
        {
            query = query.Where(m => m.Date <= to);
        }

        var rows = await query.OrderByDescending(m => m.Date).ToListAsync(ct);
        return rows.Select(MeasurementDto.From).ToList();
    }
}
