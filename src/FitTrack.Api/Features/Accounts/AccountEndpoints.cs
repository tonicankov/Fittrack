using System.Security.Claims;
using System.Text.Json;
using FitTrack.Api.Auth;
using Microsoft.Extensions.Options;

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

        me.MapGet("/export", async (
            ClaimsPrincipal user,
            ExportService export,
            IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> json,
            TimeProvider time,
            CancellationToken ct) =>
        {
            var data = await export.ExportAsync(user.GetUserId(), ct);
            var options = new JsonSerializerOptions(json.Value.SerializerOptions) { WriteIndented = true };
            var fileName = $"fittrack-export-{time.GetUtcNow():yyyy-MM-dd}.json";
            return Results.File(JsonSerializer.SerializeToUtf8Bytes(data, options), "application/json", fileName);
        });

        me.MapDelete("", async (ClaimsPrincipal user, AccountService accounts, CancellationToken ct) =>
        {
            await accounts.DeleteAccountAsync(user.GetUserId(), ct);
            return Results.NoContent();
        });

        return app;
    }
}
