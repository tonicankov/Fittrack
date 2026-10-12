using System.Security.Claims;
using FitTrack.Api.Auth;

namespace FitTrack.Api.Features.Meals;

public static class MealEndpoints
{
    public static IEndpointRouteBuilder MapMealEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/meals").WithTags("Meals");

        group.MapGet("", async (DateOnly? date, Guid? userId, ClaimsPrincipal user, MealService meals, CancellationToken ct) =>
        {
            var me = user.GetUserId();
            return Results.Ok(await meals.ListAsync(me, userId ?? me, date, ct));
        });

        group.MapPost("", async (MealRequest request, ClaimsPrincipal user, MealService meals, CancellationToken ct) =>
        {
            var meal = await meals.CreateAsync(user.GetUserId(), request, ct);
            return Results.Created($"/api/meals/{meal.Id}", meal);
        });

        group.MapPut("/{id:guid}", async (Guid id, MealRequest request, ClaimsPrincipal user, MealService meals, CancellationToken ct) =>
            Results.Ok(await meals.UpdateAsync(user.GetUserId(), id, request, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, MealService meals, CancellationToken ct) =>
        {
            await meals.DeleteAsync(user.GetUserId(), id, ct);
            return Results.NoContent();
        });

        return app;
    }
}
