namespace AssetManagement.Infrastructure.Persistence;

public enum DatabaseProvider
{
    /// <summary>File-based SQLite. Used for local development and automated tests.</summary>
    Sqlite = 0,

    /// <summary>SQL Server / Azure SQL Database. Used in production.</summary>
    SqlServer = 1,
}

/// <summary>Bound from the <c>Database</c> configuration section.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;

    /// <summary>Name of the entry under <c>ConnectionStrings</c> to use.</summary>
    public string ConnectionStringName { get; set; } = "DefaultConnection";
}
