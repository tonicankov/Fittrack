using FitTrack.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// The connection string is read when the DbContext is created rather than here,
// so tests and Docker can supply it through configuration.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
        ?? throw new InvalidOperationException("Missing connection string 'ConnectionStrings:Default'.");
    options.UseMySQL(connectionString);
});

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // OpenAPI document at /openapi/v1.json
    app.MapOpenApi();
}

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { name = "FitTrack API", status = "running" }))
   .ExcludeFromDescription();

app.Run();
