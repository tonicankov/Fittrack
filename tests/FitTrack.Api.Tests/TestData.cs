using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FitTrack.Api.Domain;
using FitTrack.Api.Features.Accounts;
using FitTrack.Api.Features.Invites;

namespace FitTrack.Api.Tests;

public sealed record TestUser(Guid Id, string Email, string Password, string Token, HttpClient Client);

public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"{(int)response.StatusCode} {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

        return (await response.Content.ReadFromJsonAsync<T>(Options))!;
    }
}

public static class TestData
{
    public const string Password = "correct horse battery";

    /// <summary>Inserts an invite straight into the database, as if <paramref name="createdBy"/> had created it.</summary>
    public static async Task<string> CreateInviteAsync(this ApiFactory factory, Guid? createdBy = null, DateTime? expiresAt = null)
    {
        await using var db = factory.CreateDbContext();
        var invite = new Invite
        {
            Code = InviteCodes.Generate(),
            CreatedById = createdBy,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
        };
        db.Invites.Add(invite);
        await db.SaveChangesAsync();
        return invite.Code;
    }

    /// <summary>
    /// Registers a new member and returns a client that is logged in as them.
    /// Members registered without <paramref name="invitedBy"/> are strangers to each other.
    /// </summary>
    public static async Task<TestUser> RegisterAsync(this ApiFactory factory, string name = "Tester", TestUser? invitedBy = null)
    {
        var code = await factory.CreateInviteAsync(invitedBy?.Id);
        var email = $"{Guid.NewGuid():N}@example.com";
        var client = factory.CreateClient();

        var auth = await (await client.PostAsJsonAsync("/api/auth/register", new
        {
            inviteCode = code,
            email,
            displayName = name,
            password = Password,
            timeZone = "Europe/Amsterdam",
        })).ReadAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return new TestUser(auth.User.Id, email, Password, auth.AccessToken, client);
    }

    public static object Workout(string name = "Leg day", string? visibility = null, DateTimeOffset? performedAt = null) => new
    {
        name,
        performedAt,
        durationMinutes = 45,
        visibility,
        exercises = new object[]
        {
            new { name = "Squat", kind = "Strength", sets = new[] { new { reps = 5, weightKg = 80m }, new { reps = 5, weightKg = 85m } } },
            new { name = "Bike", kind = "Cardio", durationSeconds = 600, distanceKm = 4.2m },
        },
    };

    public static object Meal(string name = "Oats", int calories = 400, decimal? proteinG = 20, DateTimeOffset? eatenAt = null, string? visibility = null) => new
    {
        name,
        type = "Breakfast",
        eatenAt,
        calories,
        proteinG,
        visibility,
    };

    public static async Task<T> PostAsync<T>(this HttpClient client, string url, object body) =>
        await (await client.PostAsJsonAsync(url, body)).ReadAsync<T>();

    public static async Task<T> PutAsync<T>(this HttpClient client, string url, object body) =>
        await (await client.PutAsJsonAsync(url, body)).ReadAsync<T>();

    public static async Task<T> GetAsync<T>(this HttpClient client, string url) =>
        await (await client.GetAsync(url)).ReadAsync<T>();
}
