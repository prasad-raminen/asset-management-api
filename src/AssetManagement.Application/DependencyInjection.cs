using AssetManagement.Application.Assets;
using AssetManagement.Application.Auth;
using AssetManagement.Application.Checkouts;
using AssetManagement.Application.Maintenance;
using AssetManagement.Application.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssetManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Scoped);

        return services;
    }
}
