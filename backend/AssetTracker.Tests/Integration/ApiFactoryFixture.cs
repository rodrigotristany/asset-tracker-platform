using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AssetTracker.Tests.Integration;

public class ApiFactoryFixture : WebApplicationFactory<Program>
{
    public string ConnectionString { get; set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ConnectionString,
                ["Jwt:Key"] = "a-test-signing-key-that-is-at-least-32-bytes-long",
                ["Jwt:Issuer"] = "AssetTrackerApi",
                ["Jwt:Audience"] = "AssetTrackerDashboard",
                ["Jwt:ExpiryMinutes"] = "60"
            });
        });
    }
}
