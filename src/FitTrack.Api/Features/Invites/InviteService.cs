using FitTrack.Api.Data;
using FitTrack.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Features.Invites;

public sealed record InviteDto(
    Guid Id,
    string Code,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    bool IsUsed,
    bool IsExpired,
    string? UsedByName,
    DateTime? UsedAt);

public sealed class InviteService(AppDbContext db, TimeProvider time, ILogger<InviteService> logger)
{
    public static readonly TimeSpan Validity = TimeSpan.FromDays(7);

    public async Task<InviteDto> CreateAsync(Guid userId, CancellationToken ct)
    {
        var invite = NewInvite(userId);
        db.Invites.Add(invite);
        await db.SaveChangesAsync(ct);
        return ToDto(invite, usedByName: null);
    }

    public async Task<List<InviteDto>> ListAsync(Guid userId, CancellationToken ct)
    {
        var rows = await db.Invites
            .Where(i => i.CreatedById == userId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                Invite = i,
                UsedByName = db.Users.Where(u => u.Id == i.UsedById).Select(u => u.DisplayName).FirstOrDefault(),
            })
            .ToListAsync(ct);

        return rows.Select(r => ToDto(r.Invite, r.UsedByName)).ToList();
    }

    /// <summary>The invite with this code, if it exists, is unused and has not expired.</summary>
    public async Task<Invite?> FindRedeemableAsync(string? code, CancellationToken ct)
    {
        var normalized = InviteCodes.Normalize(code);
        if (normalized.Length == 0)
        {
            return null;
        }

        var now = time.GetUtcNow().UtcDateTime;
        var invite = await db.Invites.SingleOrDefaultAsync(i => i.Code == normalized, ct);
        return invite is { UsedById: null } && invite.ExpiresAt > now ? invite : null;
    }

    /// <summary>
    /// Registration needs an invite, so the very first account needs one that nobody issued.
    /// While no account exists, this makes sure there is one and writes it to the log.
    /// </summary>
    public async Task<string?> EnsureBootstrapInviteAsync(CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return null;
        }

        var now = time.GetUtcNow().UtcDateTime;
        var invite = await db.Invites
            .Where(i => i.CreatedById == null && i.UsedById == null && i.ExpiresAt > now)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (invite is null)
        {
            invite = NewInvite(createdBy: null);
            db.Invites.Add(invite);
            await db.SaveChangesAsync(ct);
        }

        logger.LogWarning(
            "No accounts exist yet. Register the first account with invite code {Code} (valid until {ExpiresAt:u}).",
            invite.Code, invite.ExpiresAt);
        return invite.Code;
    }

    private Invite NewInvite(Guid? createdBy)
    {
        var now = time.GetUtcNow().UtcDateTime;
        return new Invite
        {
            Code = InviteCodes.Generate(),
            CreatedById = createdBy,
            CreatedAt = now,
            ExpiresAt = now + Validity,
        };
    }

    private InviteDto ToDto(Invite invite, string? usedByName) => new(
        invite.Id,
        invite.Code,
        invite.CreatedAt,
        invite.ExpiresAt,
        IsUsed: invite.UsedById is not null || invite.UsedAt is not null,
        IsExpired: invite.UsedAt is null && invite.ExpiresAt <= time.GetUtcNow().UtcDateTime,
        usedByName,
        invite.UsedAt);
}
