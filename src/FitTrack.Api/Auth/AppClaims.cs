using System.Security.Claims;

namespace FitTrack.Api.Auth;

public static class AppClaims
{
    public const string UserId = "sub";
    public const string Name = "name";

    /// <summary>The id of the authenticated member. Only valid on endpoints that require authentication.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirst(UserId)?.Value, out var id)
            ? id
            : throw new InvalidOperationException("The access token has no user id.");
}
