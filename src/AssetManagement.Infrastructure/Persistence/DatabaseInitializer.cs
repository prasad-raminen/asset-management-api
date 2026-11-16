using AssetManagement.Application.Abstractions.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AssetManagement.Infrastructure.Persistence;

/// <summary>
/// Brings the database schema up to date and applies optional seed data.
/// SQL Server uses EF Core migrations; SQLite (dev/test) creates the schema directly from the model.
/// </summary>
public sealed class DatabaseInitializer
{
    private readonly AppDbContext _db;
    private readonly DatabaseOptions _databaseOptions;
    private readonly SeedOptions _seedOptions;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _clock;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        AppDbContext db,
        DatabaseOptions databaseOptions,
        IOptions<SeedOptions> seedOptions,
        IPasswordHasher passwordHasher,
        TimeProvider clock,
        ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _databaseOptions = databaseOptions;
        _seedOptions = seedOptions.Value;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_databaseOptions.Provider == DatabaseProvider.SqlServer)
        {
            _logger.LogInformation("Applying EF Core migrations");
            await _db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await _db.Database.EnsureCreatedAsync(cancellationToken);
        }

        await SeedAdminAsync(cancellationToken);
        await SeedSampleAssetsAsync(cancellationToken);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_seedOptions.AdminEmail) || string.IsNullOrWhiteSpace(_seedOptions.AdminPassword))
        {
            return;
        }

        var email = User.NormalizeEmail(_seedOptions.AdminEmail);
        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return;
        }

        var admin = new User(email, _seedOptions.AdminFullName, UserRole.Admin);
        admin.SetPasswordHash(_passwordHasher.Hash(_seedOptions.AdminPassword));
        _db.Users.Add(admin);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded admin account {AdminEmail}", email);
    }

    private async Task SeedSampleAssetsAsync(CancellationToken cancellationToken)
    {
        if (!_seedOptions.SampleData || await _db.Assets.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        _db.Assets.AddRange(
            new LaserScanner("RTC360 Scanner #1", "RTC-360-0001", "Leica Geosystems", "RTC360", "Hyderabad Depot", 130, now),
            new LaserScanner("BLK360 Scanner #1", "BLK-360-0001", "Leica Geosystems", "BLK360 G2", "Pune Depot", 45, now),
            new Drone("Survey Drone Alpha", "UAV-M350-0001", "DJI", "Matrice 350 RTK", "Hyderabad Depot", 55, now),
            new Drone("Survey Drone Bravo", "UAV-M350-0002", "DJI", "Matrice 350 RTK", "Pune Depot", 55, now),
            new LaserTracker("Absolute Tracker #1", "AT-960-0001", "Leica Geosystems", "AT960", "Metrology Lab", 15, now));
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded sample assets");
    }
}
