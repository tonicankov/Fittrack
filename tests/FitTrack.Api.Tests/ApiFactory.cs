using FitTrack.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Testcontainers.MySql;

namespace FitTrack.Api.Tests;

/// <summary>
/// Starts the API against a throwaway MySQL container. One instance is shared by all
/// test classes in <see cref="ApiCollection"/>, so the container starts only once.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MySqlContainer _mysql = new MySqlBuilder("mysql:8.4").Build();

    public string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync()
    {
        await _mysql.StartAsync();
        ConnectionString = new MySqlConnectionStringBuilder(_mysql.GetConnectionString())
        {
            AllowPublicKeyRetrieval = true,
        }.ConnectionString;

        // Touching Server starts the app, which creates the schema once for all tests.
        _ = Server;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = ConnectionString,
        }));
    }

    /// <summary>A context that talks to the test database directly, bypassing the API.</summary>
    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseMySQL(ConnectionString).Options);

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _mysql.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
