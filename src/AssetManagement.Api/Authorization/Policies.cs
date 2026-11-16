using AssetManagement.Domain.Users;
using Microsoft.AspNetCore.Authorization;

namespace AssetManagement.Api.Authorization;

/// <summary>Named authorization policies mapped onto the three application roles.</summary>
public static class Policies
{
    /// <summary>Any signed-in user (Viewer, Technician or Admin).</summary>
    public const string CanView = nameof(CanView);

    /// <summary>Technicians and Admins: checkout/return and maintenance.</summary>
    public const string CanOperate = nameof(CanOperate);

    /// <summary>Admins only: asset and user administration.</summary>
    public const string CanAdminister = nameof(CanAdminister);

    public static void Configure(AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddPolicy(CanView, policy => policy.RequireRole(
            nameof(UserRole.Viewer), nameof(UserRole.Technician), nameof(UserRole.Admin)));
        options.AddPolicy(CanOperate, policy => policy.RequireRole(
            nameof(UserRole.Technician), nameof(UserRole.Admin)));
        options.AddPolicy(CanAdminister, policy => policy.RequireRole(nameof(UserRole.Admin)));

        // Secure by default: every endpoint needs an authenticated user unless marked [AllowAnonymous].
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    }
}
