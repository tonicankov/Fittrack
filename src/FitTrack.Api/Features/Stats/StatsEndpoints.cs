using System.Security.Claims;
using FitTrack.Api.Auth;

namespace FitTrack.Api.Features.Stats;

public static class StatsEndpoints
{
    public static IEndpointRouteBuilder MapStatsEndpoints(this IEndpointRouteBuilder app)
    {
        // No ?userId= on purpose: the dashboard is only ever the member's own
        var group = app.MapGroup("/api/stats").WithTags("Dashboard");

        group.MapGet("/weight", async (string? range, ClaimsPrincipal user, StatsService stats, CancellationToken ct) =>
            Results.Ok(await stats.GetWeightTrendAsync(user.GetUserId(), range, ct)));

        group.MapGet("/workouts-per-week", async (int? weeks, ClaimsPrincipal user, StatsService stats, CancellationToken ct) =>
            Results.Ok(await stats.GetWorkoutsPerWeekAsync(user.GetUserId(), weeks, ct)));

        group.MapGet("/volume", async (int? weeks, ClaimsPrincipal user, StatsService stats, CancellationToken ct) =>
            Results.Ok(await stats.GetWeeklyVolumeAsync(user.GetUserId(), weeks, ct)));

        group.MapGet("/nutrition", async (int? days, ClaimsPrincipal user, StatsService stats, CancellationToken ct) =>
            Results.Ok(await stats.GetDailyNutritionAsync(user.GetUserId(), days, ct)));

        return app;
    }
}
