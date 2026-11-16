using System.ComponentModel.DataAnnotations;

namespace AssetManagement.Infrastructure.Security;

/// <summary>
/// Bound from the <c>Jwt</c> configuration section. The signing key in appsettings is a
/// development-only placeholder: in production supply it from Azure Key Vault (or an
/// environment variable such as <c>Jwt__SigningKey</c>), never from source control.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>HMAC-SHA256 key; must be at least 32 characters (256 bits).</summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 24 * 60)]
    public int ExpiryMinutes { get; set; } = 60;
}
