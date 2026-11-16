using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace AssetManagement.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API in-process against a throw-away SQLite database file.
/// Settings are applied with <c>UseSetting</c> so they are visible while Program.cs registers services.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@tests.local";
    public const string AdminPassword = "Admin-Passw0rd-123";

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"asset-api-tests-{Guid.NewGuid():N}.db");

    /// <summary>Per-IP login/register limit. High by default so ordinary tests are never throttled.</summary>
    protected virtual int AuthPermitLimit => 10_000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath}");
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-0123456789-abcdefghijklmnop");
        builder.UseSetting("Seed:AdminEmail", AdminEmail);
        builder.UseSetting("Seed:AdminPassword", AdminPassword);
        builder.UseSetting("Seed:SampleData", "false");
        builder.UseSetting("RateLimiting:GlobalPermitLimit", "10000");
        builder.UseSetting("RateLimiting:AuthPermitLimit", AuthPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        SqliteConnection.ClearAllPools();
        foreach (var file in new[] { _databasePath, _databasePath + "-shm", _databasePath + "-wal" })
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
