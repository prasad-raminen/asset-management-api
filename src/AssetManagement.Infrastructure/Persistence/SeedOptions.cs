namespace AssetManagement.Infrastructure.Persistence;

/// <summary>Bound from the <c>Seed</c> configuration section. Intended for local development and demos.</summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>When set (together with <see cref="AdminPassword"/>), an Admin account is created on startup if missing.</summary>
    public string? AdminEmail { get; set; }

    public string? AdminPassword { get; set; }

    public string AdminFullName { get; set; } = "System Administrator";

    /// <summary>Adds a handful of demo assets when the asset table is empty.</summary>
    public bool SampleData { get; set; }
}
