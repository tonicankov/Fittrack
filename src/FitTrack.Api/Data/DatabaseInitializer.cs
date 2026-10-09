using Microsoft.EntityFrameworkCore;

namespace FitTrack.Api.Data;

public static class DatabaseInitializer
{
    /// <summary>
    /// Creates the schema if it does not exist yet. MySQL in Docker can take a few
    /// seconds to accept connections, so this retries for about half a minute.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger, CancellationToken ct = default)
    {
        const int maxAttempts = 10;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var created = await db.Database.EnsureCreatedAsync(ct);
                logger.LogInformation(created ? "Database schema created." : "Database schema already exists.");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && !ct.IsCancellationRequested)
            {
                logger.LogWarning("Database not reachable yet (attempt {Attempt}/{Max}): {Message}", attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
    }
}
