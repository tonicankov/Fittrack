using System.Security.Claims;
using FitTrack.Api.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FitTrack.Api.Auth;

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider time)
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateToken(User user)
    {
        var jwt = options.Value;
        var now = time.GetUtcNow().UtcDateTime;
        var expires = now.AddDays(jwt.LifetimeDays);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(AppClaims.UserId, user.Id.ToString()),
                new Claim(AppClaims.Name, user.DisplayName),
            }),
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expires);
    }
}
