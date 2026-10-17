using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitTrack.Api.Domain;
using FitTrack.Api.Features.Accounts;
using FitTrack.Api.Features.Invites;

namespace FitTrack.Api.Tests;

[Collection(ApiCollection.Name)]
public class AuthTests(ApiFactory factory)
{
    private static object Registration(string code, string? email = null) => new
    {
        inviteCode = code,
        email = email ?? $"{Guid.NewGuid():N}@example.com",
        displayName = "Nadia",
        password = TestData.Password,
        timeZone = "Europe/Amsterdam",
    };

    [Fact]
    public async Task Registering_with_an_invite_returns_a_token_and_profile()
    {
        var code = await factory.CreateInviteAsync();

        var auth = await (await factory.CreateClient().PostAsJsonAsync("/api/auth/register", Registration(code)))
            .ReadAsync<AuthResponse>();

        Assert.False(string.IsNullOrEmpty(auth.AccessToken));
        Assert.Equal("Nadia", auth.User.DisplayName);
        Assert.Equal(Visibility.Friends, auth.User.DefaultVisibility);
    }

    [Fact]
    public async Task Registering_with_an_unknown_code_is_rejected()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", Registration("NOPE2345"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("inviteCode", out _));
    }

    [Fact]
    public async Task An_invite_code_works_only_once()
    {
        var code = await factory.CreateInviteAsync();
        var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync("/api/auth/register", Registration(code));
        var second = await client.PostAsJsonAsync("/api/auth/register", Registration(code));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task An_expired_invite_code_is_rejected()
    {
        var code = await factory.CreateInviteAsync(expiresAt: DateTime.UtcNow.AddMinutes(-1));

        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", Registration(code));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_email_address_can_only_be_registered_once()
    {
        var member = await factory.RegisterAsync();
        var code = await factory.CreateInviteAsync();

        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/register", Registration(code, member.Email.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_succeeds_with_the_right_password_only()
    {
        var member = await factory.RegisterAsync();
        var client = factory.CreateClient();

        var ok = await client.PostAsJsonAsync("/api/auth/login", new { email = member.Email, password = TestData.Password });
        var wrong = await client.PostAsJsonAsync("/api/auth/login", new { email = member.Email, password = "wrong password" });

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
    }

    [Fact]
    public async Task Endpoints_require_a_token()
    {
        var response = await factory.CreateClient().GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Members_can_update_their_profile()
    {
        var member = await factory.RegisterAsync();

        var updated = await (await member.Client.PutAsJsonAsync("/api/me", new
        {
            displayName = "Toni",
            timeZone = "Europe/London",
            defaultVisibility = "Private",
        })).ReadAsync<MeDto>();

        Assert.Equal("Toni", updated.DisplayName);
        Assert.Equal("Europe/London", updated.TimeZone);
        Assert.Equal(Visibility.Private, updated.DefaultVisibility);
    }

    [Fact]
    public async Task An_unknown_time_zone_is_rejected()
    {
        var member = await factory.RegisterAsync();

        var response = await member.Client.PutAsJsonAsync("/api/me", new
        {
            displayName = "Toni",
            timeZone = "Mars/Olympus_Mons",
            defaultVisibility = "Friends",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_deleted_account_can_no_longer_log_in()
    {
        var member = await factory.RegisterAsync();

        var delete = await member.Client.DeleteAsync("/api/me");
        var login = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new { email = member.Email, password = TestData.Password });

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Invites_show_who_used_them()
    {
        var inviter = await factory.RegisterAsync("Toni");
        var invite = await (await inviter.Client.PostAsync("/api/invites", null)).ReadAsync<InviteDto>();

        await (await factory.CreateClient().PostAsJsonAsync("/api/auth/register", Registration(invite.Code)))
            .ReadAsync<AuthResponse>();

        var invites = await (await inviter.Client.GetAsync("/api/invites")).ReadAsync<List<InviteDto>>();
        var used = Assert.Single(invites);
        Assert.True(used.IsUsed);
        Assert.Equal("Nadia", used.UsedByName);
    }

    [Fact]
    public async Task Members_can_download_all_their_data()
    {
        var member = await factory.RegisterAsync("Toni");
        await member.Client.PostAsJsonAsync("/api/workouts", TestData.Workout("Leg day"));

        var response = await member.Client.GetAsync("/api/me/export");
        var export = await response.ReadAsync<ExportDto>();

        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("Toni", export.Profile.DisplayName);
        Assert.Equal("Leg day", Assert.Single(export.Workouts).Name);
    }
}
