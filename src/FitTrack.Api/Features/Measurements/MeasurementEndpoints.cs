using System.Security.Claims;
using FitTrack.Api.Auth;

namespace FitTrack.Api.Features.Measurements;

public static class MeasurementEndpoints
{
    public static IEndpointRouteBuilder MapMeasurementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/measurements").WithTags("Body measurements");

        group.MapGet("", async (DateOnly? from, DateOnly? to, Guid? userId, ClaimsPrincipal user, MeasurementService measurements, CancellationToken ct) =>
        {
            var me = user.GetUserId();
            return Results.Ok(await measurements.ListAsync(me, userId ?? me, from, to, ct));
        });

        // PUT because the date identifies the measurement: recording a day twice replaces it
        group.MapPut("/{date}", async (DateOnly date, MeasurementRequest request, ClaimsPrincipal user, MeasurementService measurements, CancellationToken ct) =>
            Results.Ok(await measurements.UpsertAsync(user.GetUserId(), date, request, ct)));

        group.MapDelete("/{date}", async (DateOnly date, ClaimsPrincipal user, MeasurementService measurements, CancellationToken ct) =>
        {
            await measurements.DeleteAsync(user.GetUserId(), date, ct);
            return Results.NoContent();
        });

        return app;
    }
}
