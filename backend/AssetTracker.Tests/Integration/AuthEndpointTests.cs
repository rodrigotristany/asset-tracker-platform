using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    public async Task Login_WithWrongPassword_ReturnsUnauthorizedWithInvalidCredentialsError()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequestDto { Username = "admin", Password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("INVALID_CREDENTIALS", body!["error"].GetString());
    }

    [Fact]
    public async Task Login_WithMissingUsername_ReturnsValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequestDto { Username = string.Empty, Password = "ChangeMe123!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("VALIDATION_ERROR", body!["error"].GetString());
    }

    [Fact]
    public async Task Login_WithMissingPassword_ReturnsValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequestDto { Username = "admin", Password = string.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("VALIDATION_ERROR", body!["error"].GetString());
    }
}
