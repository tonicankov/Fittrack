var builder = WebApplication.CreateBuilder(args);

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
