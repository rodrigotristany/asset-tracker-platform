using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
    public async Task Register_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/devices",
            new DeviceRegisterRequestDto { DeviceId = "unauthorized-attempt" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
    }
}
