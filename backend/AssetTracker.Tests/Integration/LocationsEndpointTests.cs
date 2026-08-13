using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AssetTracker.Application.Dtos;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class LocationsEndpointTests : IClassFixture<ApiFactoryFixture>
{
    private readonly HttpClient _client;

    public LocationsEndpointTests(SqlServerFixture dbFixture, ApiFactoryFixture apiFixture)
    {
        apiFixture.ConnectionString = dbFixture.ConnectionString;
        _client = apiFixture.CreateClient();
    }

    private async Task<(string DeviceId, string ApiKey)> RegisterDeviceAsync()
    {
        var token = await TestAuthHelper.GetAdminJwtAsync(_client);
        var deviceId = $"test-device-{Guid.NewGuid():N}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/devices")
        {
            Content = JsonContent.Create(new DeviceRegisterRequestDto { DeviceId = deviceId })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<DeviceRegisterResponseDto>();

        return (deviceId, body!.ApiKey);
    }

    [Fact]
    public async Task Create_WithoutApiKey_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/locations",
            new LocationCreateDto { DeviceId = "does-not-matter", Timestamp = DateTimeOffset.UtcNow, Latitude = 1, Longitude = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidApiKey_ReturnsCreated()
    {
        var (deviceId, apiKey) = await RegisterDeviceAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/locations")
        {
            Content = JsonContent.Create(new LocationCreateDto
            {
                DeviceId = deviceId,
                Timestamp = DateTimeOffset.UtcNow,
                Latitude = -31.4231,
                Longitude = -62.0834,
                Altitude = 142.1,
                Speed = 0.4,
                Satellites = 9,
                Hdop = 0.8,
                BatteryVoltage = 3.7,
                IsStale = false
            })
        };
        request.Headers.Add("X-API-Key", apiKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidLatitude_ReturnsValidationError()
    {
        var (deviceId, apiKey) = await RegisterDeviceAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/locations")
        {
            Content = JsonContent.Create(new LocationCreateDto
            {
                DeviceId = deviceId,
                Timestamp = DateTimeOffset.UtcNow,
                Latitude = 999,
                Longitude = 1
            })
        };
        request.Headers.Add("X-API-Key", apiKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("VALIDATION_ERROR", body!["error"].GetString());
    }

    [Fact]
    public async Task CreateBatch_WithValidApiKey_ReturnsCreatedWithAllItems()
    {
        var (deviceId, apiKey) = await RegisterDeviceAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/locations/batch")
        {
            Content = JsonContent.Create(new LocationBatchCreateDto
            {
                DeviceId = deviceId,
                Locations = new List<LocationCreateDto>
                {
                    new() { DeviceId = deviceId, Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1), Latitude = 1, Longitude = 1 },
                    new() { DeviceId = deviceId, Timestamp = DateTimeOffset.UtcNow, Latitude = 2, Longitude = 2 }
                }
            })
        };
        request.Headers.Add("X-API-Key", apiKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<LocationCreateResponseDto>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetLatestByDevice_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/locations/some-device");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLatestByDevice_WithValidJwt_ReturnsLatestLocation()
    {
        var (deviceId, apiKey) = await RegisterDeviceAsync();

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/locations")
        {
            Content = JsonContent.Create(new LocationCreateDto
            {
                DeviceId = deviceId,
                Timestamp = DateTimeOffset.UtcNow,
                Latitude = 5,
                Longitude = 6
            })
        };
        createRequest.Headers.Add("X-API-Key", apiKey);
        (await _client.SendAsync(createRequest)).EnsureSuccessStatusCode();

        var token = await TestAuthHelper.GetAdminJwtAsync(_client);
        using var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/locations/{deviceId}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(getRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<LocationReadDto>>();
        Assert.Single(body!);
        Assert.Equal(deviceId, body[0].DeviceId);
    }
}
