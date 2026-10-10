using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FitTrack.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "FitTrack";

    public string Audience { get; set; } = "FitTrack";

    /// <summary>HMAC key, at least 32 characters. Supplied through configuration, never committed for production.</summary>
    public string SigningKey { get; set; } = "";

    public int LifetimeDays { get; set; } = 7;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
