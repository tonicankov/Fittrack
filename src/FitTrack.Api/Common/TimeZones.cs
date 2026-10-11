namespace FitTrack.Api.Common;

/// <summary>
/// Timestamps are stored in UTC. "A day" and "a week" are always evaluated in the member's own time zone.
/// </summary>
public static class TimeZones
{
    public const string Default = "Europe/Amsterdam";

    public static bool IsValid(string? id) => id is not null && TryFind(id, out _);

    /// <summary>The time zone with this IANA id, or UTC if it is unknown on this machine.</summary>
    public static TimeZoneInfo Find(string? id) => id is not null && TryFind(id, out var tz) ? tz : TimeZoneInfo.Utc;

    public static DateOnly LocalDate(DateTime utc, TimeZoneInfo tz) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz));

    public static DateOnly Today(TimeProvider time, TimeZoneInfo tz) => LocalDate(time.GetUtcNow().UtcDateTime, tz);

    /// <summary>The UTC moment at which <paramref name="date"/> starts in <paramref name="tz"/>.</summary>
    public static DateTime StartOfDayUtc(DateOnly date, TimeZoneInfo tz)
    {
        var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        if (tz.IsInvalidTime(local))
        {
            // Some zones skip midnight when daylight saving starts
            local = local.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, tz);
    }

    /// <summary>Monday of the ISO week that contains <paramref name="date"/>.</summary>
    public static DateOnly WeekStart(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    private static bool TryFind(string id, out TimeZoneInfo tz)
    {
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            tz = TimeZoneInfo.Utc;
            return false;
        }
    }
}
