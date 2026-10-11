using FitTrack.Api.Domain;

namespace FitTrack.Api.Features.Accounts;

public sealed record RegisterRequest(string? InviteCode, string? Email, string? DisplayName, string? Password, string? TimeZone);

public sealed record LoginRequest(string? Email, string? Password);

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, MeDto User);

public sealed record MeDto(Guid Id, string Email, string DisplayName, string TimeZone, Visibility DefaultVisibility, DateTime CreatedAt)
{
    public static MeDto From(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.TimeZone, user.DefaultVisibility, user.CreatedAt);
}
