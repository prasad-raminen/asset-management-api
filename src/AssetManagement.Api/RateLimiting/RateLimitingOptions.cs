namespace AssetManagement.Api.RateLimiting;

/// <summary>Bound from the <c>RateLimiting</c> configuration section.</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Requests allowed per window for each user (or client IP when anonymous).</summary>
    public int GlobalPermitLimit { get; set; } = 100;

    public int GlobalWindowSeconds { get; set; } = 60;

    /// <summary>Stricter limit for login/register to slow down credential stuffing.</summary>
    public int AuthPermitLimit { get; set; } = 10;

    public int AuthWindowSeconds { get; set; } = 60;
}

public static class RateLimitPolicies
{
    public const string Auth = "auth";
}
