using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
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
    public async Task Create_WithoutApiKey_ReturnsUnauthorizedWithEnvelope()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/locations",
            new LocationCreateDto { DeviceId = "does-not-matter", Timestamp = DateTimeOffset.UtcNow, Latitude = 1, Longitude = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("AUTHENTICATION_REQUIRED", body!["error"].GetString());
    }

    [Fact]
    public async Task Create_WithMalformedApiKey_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/locations")
        {
            Content = JsonContent.Create(new LocationCreateDto
            {
                DeviceId = "does-not-matter", Timestamp = DateTimeOffset.UtcNow, Latitude = 1, Longitude = 1
            })
        };
        // Not valid base64 - ApiKeyAuthenticationHandler must reject this cleanly, not 500.
        request.Headers.Add("X-API-Key", "not-valid-base64!!!");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("AUTHENTICATION_REQUIRED", body!["error"].GetString());
    }

    [Fact]
    public async Task Create_WithUnregisteredApiKey_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/locations")
        {
            Content = JsonContent.Create(new LocationCreateDto
            {
                DeviceId = "does-not-matter", Timestamp = DateTimeOffset.UtcNow, Latitude = 1, Longitude = 1
            })
        };
        // Well-formed base64, but not a key that was ever issued to a registered device.
        request.Headers.Add("X-API-Key", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("AUTHENTICATION_REQUIRED", body!["error"].GetString());
    }

    [Fact]
    public async Task Create_WithMismatchedDeviceId_ReturnsForbidden()
    {
        var (_, apiKeyOfDeviceB) = await RegisterDeviceAsync();
        var (deviceIdOfDeviceA, _) = await RegisterDeviceAsync();

        // Device B's API key must not be usable to write location history claiming to be
        // device A - that would let any registered device forge another device's data.
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/locations")
        {
            Content = JsonContent.Create(new LocationCreateDto
            {
                DeviceId = deviceIdOfDeviceA,
                Timestamp = DateTimeOffset.UtcNow,
                Latitude = 1,
                Longitude = 1
            })
        };
        request.Headers.Add("X-API-Key", apiKeyOfDeviceB);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("FORBIDDEN", body!["error"].GetString());
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
    public async Task CreateBatch_WithoutApiKey_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/locations/batch",
            new LocationBatchCreateDto
            {
                DeviceId = "does-not-matter",
                Locations = new List<LocationCreateDto>
                {
                    new() { DeviceId = "does-not-matter", Timestamp = DateTimeOffset.UtcNow, Latitude = 1, Longitude = 1 }
                }
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("AUTHENTICATION_REQUIRED", body!["error"].GetString());
    }

    [Fact]
    public async Task CreateBatch_WithEmptyLocations_ReturnsValidationError()
    {
        var (deviceId, apiKey) = await RegisterDeviceAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/locations/batch")
        {
            Content = JsonContent.Create(new LocationBatchCreateDto
            {
                DeviceId = deviceId,
                Locations = new List<LocationCreateDto>()
            })
        };
        request.Headers.Add("X-API-Key", apiKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("VALIDATION_ERROR", body!["error"].GetString());
    }

    [Fact]
    public async Task CreateBatch_WithMismatchedDeviceId_ReturnsForbidden()
    {
        var (_, apiKeyOfDeviceB) = await RegisterDeviceAsync();
        var (deviceIdOfDeviceA, _) = await RegisterDeviceAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/locations/batch")
        {
            Content = JsonContent.Create(new LocationBatchCreateDto
            {
                DeviceId = deviceIdOfDeviceA,
                Locations = new List<LocationCreateDto>
                {
                    new() { DeviceId = deviceIdOfDeviceA, Timestamp = DateTimeOffset.UtcNow, Latitude = 1, Longitude = 1 }
                }
            })
        };
        request.Headers.Add("X-API-Key", apiKeyOfDeviceB);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("FORBIDDEN", body!["error"].GetString());
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
        var single = Assert.Single(body!);
        Assert.Equal(deviceId, single.DeviceId);
    }
}
