using FitTrack.Api.Domain;

namespace FitTrack.Api.Features.Measurements;

public sealed record MeasurementRequest(decimal? WeightKg, decimal? BodyFatPct, decimal? WaistCm, Visibility? Visibility);

public sealed record MeasurementDto(
    Guid Id,
    Guid UserId,
    DateOnly Date,
    decimal WeightKg,
    decimal? BodyFatPct,
    decimal? WaistCm,
    Visibility Visibility,
    DateTime CreatedAt)
{
    public static MeasurementDto From(BodyMeasurement m) =>
        new(m.Id, m.UserId, m.Date, m.WeightKg, m.BodyFatPct, m.WaistCm, m.Visibility, m.CreatedAt);
}
