using AssetManagement.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace AssetManagement.Api.Extensions;

public static class WebApplicationExtensions
{
    private const string DevelopmentKeyMarker = "DEV-ONLY";

    /// <summary>
    /// Refuses to start in Production with the placeholder signing key from appsettings.json.
    /// Production keys belong in Azure Key Vault (or an app setting such as <c>Jwt__SigningKey</c>).
    /// </summary>
    public static void EnsureProductionSigningKey(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsProduction())
        {
            return;
        }

        var options = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        if (options.SigningKey.Contains(DevelopmentKeyMarker, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The development JWT signing key must not be used in Production. "
                + "Provide Jwt:SigningKey from Azure Key Vault or a secure app setting.");
        }
    }
}
