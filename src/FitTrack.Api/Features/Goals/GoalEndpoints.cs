using System.Security.Claims;
using FitTrack.Api.Auth;

namespace FitTrack.Api.Features.Goals;

public static class GoalEndpoints
{
    public static IEndpointRouteBuilder MapGoalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/goals").WithTags("Goals");

        group.MapGet("", async (Guid? userId, ClaimsPrincipal user, GoalService goals, CancellationToken ct) =>
        {
            var me = user.GetUserId();
            return Results.Ok(await goals.ListAsync(me, userId ?? me, ct));
        });

        group.MapPost("", async (CreateGoalRequest request, ClaimsPrincipal user, GoalService goals, CancellationToken ct) =>
        {
            var goal = await goals.CreateAsync(user.GetUserId(), request, ct);
            return Results.Created($"/api/goals/{goal.Id}", goal);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateGoalRequest request, ClaimsPrincipal user, GoalService goals, CancellationToken ct) =>
            Results.Ok(await goals.UpdateAsync(user.GetUserId(), id, request, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, GoalService goals, CancellationToken ct) =>
        {
            await goals.DeleteAsync(user.GetUserId(), id, ct);
            return Results.NoContent();
        });

        return app;
    }
}
