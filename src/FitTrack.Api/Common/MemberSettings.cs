using FitTrack.Api.Data;
using FitTrack.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Common;

public sealed record MemberSettings(TimeZoneInfo TimeZone, Visibility DefaultVisibility);

/// <summary>Per-request cache of the profile settings that the entry services need.</summary>
public sealed class MemberSettingsProvider(AppDbContext db)
{
    private readonly Dictionary<Guid, MemberSettings> _cache = [];

    public async Task<MemberSettings> GetAsync(Guid userId, CancellationToken ct)
    {
        if (_cache.TryGetValue(userId, out var cached))
        {
            return cached;
        }

        var row = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.TimeZone, u.DefaultVisibility })
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException();

        return _cache[userId] = new MemberSettings(TimeZones.Find(row.TimeZone), row.DefaultVisibility);
    }
}
