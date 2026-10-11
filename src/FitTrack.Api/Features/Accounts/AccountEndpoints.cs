using System.Security.Claims;
using FitTrack.Api.Auth;

namespace FitTrack.Api.Features.Accounts;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();

        auth.MapPost("/register", async (RegisterRequest request, AccountService accounts, CancellationToken ct) =>
            Results.Ok(await accounts.RegisterAsync(request, ct)));

        auth.MapPost("/login", async (LoginRequest request, AccountService accounts, CancellationToken ct) =>
            Results.Ok(await accounts.LoginAsync(request, ct)));

        var me = app.MapGroup("/api/me").WithTags("Account");

        me.MapGet("", async (ClaimsPrincipal user, AccountService accounts, CancellationToken ct) =>
            Results.Ok(await accounts.GetMeAsync(user.GetUserId(), ct)));

        me.MapPut("", async (UpdateProfileRequest request, ClaimsPrincipal user, AccountService accounts, CancellationToken ct) =>
            Results.Ok(await accounts.UpdateProfileAsync(user.GetUserId(), request, ct)));

        return app;
    }
}
