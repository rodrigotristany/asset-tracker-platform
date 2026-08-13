using System.Net;
using System.Net.Http.Json;
using AssetTracker.Application.Dtos;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class AuthEndpointTests : IClassFixture<ApiFactoryFixture>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(SqlServerFixture dbFixture, ApiFactoryFixture apiFixture)
    {
        apiFixture.ConnectionString = dbFixture.ConnectionString;
        _client = apiFixture.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequestDto { Username = "admin", Password = "ChangeMe123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequestDto { Username = "admin", Password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
