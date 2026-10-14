using System.Net;
using System.Net.Http.Json;
using FitTrack.Api.Features.Meals;
using FitTrack.Api.Features.Measurements;

namespace FitTrack.Api.Tests;

[Collection(ApiCollection.Name)]
public class MealAndMeasurementTests(ApiFactory factory)
{
    [Fact]
    public async Task Daily_totals_follow_the_members_time_zone()
    {
        var member = await factory.RegisterAsync(); // Europe/Amsterdam, UTC+2 in August

        // 23:30 local on 1 August, and 00:30 local on 2 August
        await member.Client.PostAsync<MealDto>("/api/meals", TestData.Meal("Late snack", 300, 10, DateTimeOffset.Parse("2026-08-01T21:30:00Z")));
        await member.Client.PostAsync<MealDto>("/api/meals", TestData.Meal("Midnight snack", 200, 5, DateTimeOffset.Parse("2026-08-01T22:30:00Z")));
        await member.Client.PostAsync<MealDto>("/api/meals", TestData.Meal("Lunch", 700, 40, DateTimeOffset.Parse("2026-08-01T10:00:00Z")));

        var totals = await member.Client.GetAsync<List<DailyTotalsDto>>("/api/meals/daily-totals?from=2026-08-01&to=2026-08-02");

        Assert.Equal(2, totals.Count);
        Assert.Equal(new DateOnly(2026, 8, 1), totals[0].Date);
        Assert.Equal(1000, totals[0].Calories);
        Assert.Equal(50m, totals[0].ProteinG);
        Assert.Equal(200, totals[1].Calories);
    }

    [Fact]
    public async Task Meals_are_listed_per_local_day()
    {
        var member = await factory.RegisterAsync();
        await member.Client.PostAsync<MealDto>("/api/meals", TestData.Meal("Dinner", eatenAt: DateTimeOffset.Parse("2026-08-03T17:00:00Z")));
        await member.Client.PostAsync<MealDto>("/api/meals", TestData.Meal("Other day", eatenAt: DateTimeOffset.Parse("2026-08-04T17:00:00Z")));

        var meals = await member.Client.GetAsync<List<MealDto>>("/api/meals?date=2026-08-03");

        Assert.Equal("Dinner", Assert.Single(meals).Name);
    }

    [Fact]
    public async Task Recording_a_day_twice_keeps_one_measurement()
    {
        var member = await factory.RegisterAsync();

        await member.Client.PutAsync<MeasurementDto>("/api/measurements/2026-08-01", new { weightKg = 80.4m });
        await member.Client.PutAsync<MeasurementDto>("/api/measurements/2026-08-01", new { weightKg = 80.1m, bodyFatPct = 18.5m });

        var measurements = await member.Client.GetAsync<List<MeasurementDto>>("/api/measurements");
        var only = Assert.Single(measurements);
        Assert.Equal(80.1m, only.WeightKg);
        Assert.Equal(18.5m, only.BodyFatPct);
    }

    [Fact]
    public async Task Implausible_weights_are_rejected()
    {
        var member = await factory.RegisterAsync();

        var response = await member.Client.PutAsJsonAsync("/api/measurements/2026-08-01", new { weightKg = 4000m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
