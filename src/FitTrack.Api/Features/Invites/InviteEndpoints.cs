using System.Security.Claims;
using FitTrack.Api.Auth;

namespace FitTrack.Api.Features.Invites;

public static class InviteEndpoints
{
    public static IEndpointRouteBuilder MapInviteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invites").WithTags("Invites");

        group.MapGet("", async (ClaimsPrincipal user, InviteService invites, CancellationToken ct) =>
            Results.Ok(await invites.ListAsync(user.GetUserId(), ct)));

        group.MapPost("", async (ClaimsPrincipal user, InviteService invites, CancellationToken ct) =>
        {
            var invite = await invites.CreateAsync(user.GetUserId(), ct);
            return Results.Created($"/api/invites/{invite.Id}", invite);
        });

        return app;
    }
}
