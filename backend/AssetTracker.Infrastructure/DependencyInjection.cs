using AssetTracker.Application.Interfaces;
using AssetTracker.Infrastructure.Data;
using AssetTracker.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default configuration is required.");

        services.AddDbContext<AssetTrackerDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IDeviceRepository>(sp =>
            new DeviceRepository(connectionString, sp.GetRequiredService<AssetTrackerDbContext>()));
        services.AddScoped<ILocationRepository>(_ => new LocationRepository(connectionString));
        services.AddScoped<IRetentionRepository>(_ => new RetentionRepository(connectionString));
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();

        return services;
    }
}
