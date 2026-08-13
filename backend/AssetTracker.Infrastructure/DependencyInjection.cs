using AssetTracker.Application.Interfaces;
using AssetTracker.Infrastructure.Data;
using AssetTracker.Infrastructure.Repositories;
using AssetTracker.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Connection string is read lazily (from the DI-resolved IConfiguration, at the point each
        // service is actually resolved) rather than captured eagerly into a local variable here.
        // Program.cs calls AddInfrastructure(builder.Configuration) before builder.Build() runs;
        // WebApplicationFactory-based integration tests override ConnectionStrings:Default via
        // ConfigureAppConfiguration, but that override is only merged into the live configuration
        // as part of Build(). An eager read here would capture the pre-override (appsettings.json)
        // value instead.
        static string GetConnectionString(IConfiguration configuration) =>
            configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("ConnectionStrings:Default configuration is required.");

        services.AddDbContext<AssetTrackerDbContext>((sp, options) =>
            options.UseSqlServer(GetConnectionString(sp.GetRequiredService<IConfiguration>())));

        services.AddScoped<IDeviceRepository>(sp =>
            new DeviceRepository(GetConnectionString(sp.GetRequiredService<IConfiguration>()), sp.GetRequiredService<AssetTrackerDbContext>()));
        services.AddScoped<ILocationRepository>(sp =>
            new LocationRepository(GetConnectionString(sp.GetRequiredService<IConfiguration>())));
        services.AddScoped<IRetentionRepository>(sp =>
            new RetentionRepository(GetConnectionString(sp.GetRequiredService<IConfiguration>())));
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();

        // .ValidateOnStart() restores fail-fast startup behavior for a missing/blank Jwt:Key
        // (registers a hosted service that validates options during host startup) while still
        // binding from the DI-resolved IConfiguration, keeping compatibility with
        // WebApplicationFactory's config-override timing used by integration tests.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Key), "Jwt:Key configuration is required.")
            .ValidateOnStart();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
