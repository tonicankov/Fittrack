using System.Security.Claims;
using FitTrack.Api.Auth;

namespace FitTrack.Api.Features.Feed;

public static class FeedEndpoints
{
    public static IEndpointRouteBuilder MapFeedEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/feed", async (DateTimeOffset? before, ClaimsPrincipal user, FeedService feed, CancellationToken ct) =>
                Results.Ok(await feed.GetFeedAsync(user.GetUserId(), before?.UtcDateTime, ct)))
            .WithTags("Feed");

        return app;
    }
}
