using System.Text.Json.Serialization;
using FitTrack.Api.Auth;
using FitTrack.Api.Common;
using FitTrack.Api.Data;
using FitTrack.Api.Domain;
using FitTrack.Api.Features.Accounts;
using FitTrack.Api.Features.Feed;
using FitTrack.Api.Features.Friends;
using FitTrack.Api.Features.Goals;
using FitTrack.Api.Features.Invites;
using FitTrack.Api.Features.Meals;
using FitTrack.Api.Features.Measurements;
using FitTrack.Api.Features.Workouts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// The connection string is read when the DbContext is created rather than here,
// so tests and Docker can supply it through configuration.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
        ?? throw new InvalidOperationException("Missing connection string 'ConnectionStrings:Default'.");
    options.UseMySQL(connectionString);
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<InviteService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<MemberSettingsProvider>();
builder.Services.AddScoped<WorkoutService>();
builder.Services.AddScoped<MealService>();
builder.Services.AddScoped<MeasurementService>();
builder.Services.AddScoped<GoalService>();
builder.Services.AddScoped<FriendshipService>();
builder.Services.AddScoped<VisibilityService>();
builder.Services.AddScoped<FeedService>();

// Real-time feed updates
builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IFeedNotifier, SignalRFeedNotifier>();

// Authentication: JWT bearer tokens issued by TokenService
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(o => o.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters long.")
    .ValidateOnStart();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        options.MapInboundClaims = false;
        // Browsers cannot send headers on WebSocket requests, so SignalR passes the token in the query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            },
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = jwt.CreateSigningKey(),
            NameClaimType = AppClaims.Name,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

// Every endpoint requires a logged-in member unless it opts out with AllowAnonymous()
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

if (app.Configuration.GetValue("Database:InitializeOnStartup", true))
{
    await DatabaseInitializer.InitializeAsync(app.Services, app.Logger);
}

if (app.Configuration.GetValue("Invites:BootstrapOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<InviteService>().EnsureBootstrapInviteAsync(CancellationToken.None);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    // OpenAPI document at /openapi/v1.json
    app.MapOpenApi().AllowAnonymous();
}

app.MapHealthChecks("/health").AllowAnonymous();
app.MapGet("/", () => Results.Ok(new { name = "FitTrack API", status = "running" }))
   .AllowAnonymous()
   .ExcludeFromDescription();

app.MapAccountEndpoints();
app.MapInviteEndpoints();
app.MapWorkoutEndpoints();
app.MapMealEndpoints();
app.MapMeasurementEndpoints();
app.MapGoalEndpoints();
app.MapFriendEndpoints();
app.MapFeedEndpoints();
app.MapHub<FeedHub>(FeedHub.Path);

app.Run();

// Lets the integration tests start the app with WebApplicationFactory<Program>.
public partial class Program;
