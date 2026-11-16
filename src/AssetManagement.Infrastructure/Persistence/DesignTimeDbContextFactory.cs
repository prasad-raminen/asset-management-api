using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AssetManagement.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> to generate SQL Server migrations. No database connection is opened
/// when adding a migration, so the placeholder connection string is never used.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=AssetManagement;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new AppDbContext(options, TimeProvider.System);
    }
}
