using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AssetTracker.Tests.Integration;

// Uses ApiFactoryFixture (rather than a bare WebApplicationFactory<Program>) so startup
// configuration is overridden the same way every other integration test suite overrides it.
// appsettings.json's ConnectionStrings:Default/Jwt:Key are intentionally blank in the shipped
// app (so the fail-fast ValidateOnStart()/startup checks actually fire on a real deploy that
// forgets to set the real env vars) — a bare WebApplicationFactory<Program> would otherwise fail
// to start here. The health endpoint never touches the database, so an unreachable placeholder
// connection string is fine; this test deliberately does not need the real Testcontainers-backed
// SqlServerFixture that the DB-touching endpoint tests use.
public class HealthEndpointTests : IClassFixture<ApiFactoryFixture>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(ApiFactoryFixture factory)
    {
        factory.ConnectionString = "Server=unused;Database=unused;User Id=unused;Password=unused;TrustServerCertificate=True;";
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOkWithStatus()
    {
        var response = await _client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("ok", body!["status"]);
    }
}
