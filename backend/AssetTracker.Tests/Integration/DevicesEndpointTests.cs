using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AssetTracker.Application.Dtos;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class DevicesEndpointTests : IClassFixture<ApiFactoryFixture>
{
    private readonly HttpClient _client;

    public DevicesEndpointTests(SqlServerFixture dbFixture, ApiFactoryFixture apiFixture)
    {
        apiFixture.ConnectionString = dbFixture.ConnectionString;
        _client = apiFixture.CreateClient();
    }

    [Fact]
    public async Task Register_WithoutJwt_ReturnsUnauthorizedWithEnvelope()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/devices",
            new DeviceRegisterRequestDto { DeviceId = "unauthorized-attempt" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("AUTHENTICATION_REQUIRED", body!["error"].GetString());
    }

    [Fact]
    public async Task Register_WithTamperedJwt_ReturnsUnauthorized()
    {
        var token = await TestAuthHelper.GetAdminJwtAsync(_client);
        var tamperedToken = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/devices")
        {
            Content = JsonContent.Create(new DeviceRegisterRequestDto { DeviceId = "tampered-jwt-attempt" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("AUTHENTICATION_REQUIRED", body!["error"].GetString());
    }

    [Fact]
    public async Task Register_WithGarbageJwt_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/devices")
        {
            Content = JsonContent.Create(new DeviceRegisterRequestDto { DeviceId = "garbage-jwt-attempt" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-jwt");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithValidApiKeyInsteadOfJwt_ReturnsUnauthorized()
    {
        var token = await TestAuthHelper.GetAdminJwtAsync(_client);
        var seedDeviceId = $"test-device-{Guid.NewGuid():N}";
        using var seedRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/devices")
        {
            Content = JsonContent.Create(new DeviceRegisterRequestDto { DeviceId = seedDeviceId })
        };
        seedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var seedResponse = await _client.SendAsync(seedRequest);
        seedResponse.EnsureSuccessStatusCode();
        var seedBody = await seedResponse.Content.ReadFromJsonAsync<DeviceRegisterResponseDto>();

        // A real, valid, registered device's API key should not satisfy a JWT-only endpoint -
        // the two authentication schemes must not be interchangeable (cross-scheme rejection).
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/devices")
        {
            Content = JsonContent.Create(new DeviceRegisterRequestDto { DeviceId = "cross-scheme-attempt" })
        };
        request.Headers.Add("X-API-Key", seedBody!.ApiKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithEmptyDeviceId_ReturnsValidationError()
    {
        var token = await TestAuthHelper.GetAdminJwtAsync(_client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/devices")
        {
            Content = JsonContent.Create(new DeviceRegisterRequestDto { DeviceId = string.Empty })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("VALIDATION_ERROR", body!["error"].GetString());
    }

    [Fact]
    public async Task Register_WithValidJwt_ReturnsCreatedWithApiKey()
    {
        var token = await TestAuthHelper.GetAdminJwtAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deviceId = $"test-device-{Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync("/api/v1/devices",
            new DeviceRegisterRequestDto { DeviceId = deviceId, DisplayName = "Test Device" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DeviceRegisterResponseDto>();
        Assert.Equal(deviceId, body!.DeviceId);
        Assert.False(string.IsNullOrWhiteSpace(body.ApiKey));
    }

    [Fact]
    public async Task Register_WithDuplicateDeviceId_ReturnsConflict()
    {
        var token = await TestAuthHelper.GetAdminJwtAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deviceId = $"test-device-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/v1/devices", new DeviceRegisterRequestDto { DeviceId = deviceId });

        var response = await _client.PostAsJsonAsync("/api/v1/devices", new DeviceRegisterRequestDto { DeviceId = deviceId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("DEVICE_ALREADY_EXISTS", body!["error"].GetString());
    }
}
