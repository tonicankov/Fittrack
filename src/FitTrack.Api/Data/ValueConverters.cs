using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FitTrack.Api.Data;

/// <summary>
/// MySQL's DATETIME has no time zone, so values come back with Kind = Unspecified.
/// Everything is stored in UTC, so mark it as such when reading.
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

/// <summary>Stores <see cref="DateOnly"/> as a DATE column.</summary>
public sealed class DateOnlyConverter() : ValueConverter<DateOnly, DateTime>(
    d => d.ToDateTime(TimeOnly.MinValue),
    d => DateOnly.FromDateTime(d));
