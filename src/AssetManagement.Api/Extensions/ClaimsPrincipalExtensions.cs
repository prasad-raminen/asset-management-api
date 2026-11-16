using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AssetManagement.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Returns the authenticated user's id from the <c>sub</c> claim.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("The access token does not contain a valid subject claim.");
    }
}
