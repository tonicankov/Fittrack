using System.Security.Claims;
using FitTrack.Api.Auth;

namespace FitTrack.Api.Features.Workouts;

public static class WorkoutEndpoints
{
    public static IEndpointRouteBuilder MapWorkoutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workouts").WithTags("Workouts");

        // ?userId= lists a friend's workouts; without it, your own
        group.MapGet("", async (Guid? userId, DateTimeOffset? before, ClaimsPrincipal user, WorkoutService workouts, CancellationToken ct) =>
        {
            var me = user.GetUserId();
            return Results.Ok(await workouts.ListAsync(me, userId ?? me, before?.UtcDateTime, ct));
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, WorkoutService workouts, CancellationToken ct) =>
            Results.Ok(await workouts.GetAsync(user.GetUserId(), id, ct)));

        group.MapPost("", async (WorkoutRequest request, ClaimsPrincipal user, WorkoutService workouts, CancellationToken ct) =>
        {
            var workout = await workouts.CreateAsync(user.GetUserId(), request, ct);
            return Results.Created($"/api/workouts/{workout.Id}", workout);
        });

        return app;
    }
}
