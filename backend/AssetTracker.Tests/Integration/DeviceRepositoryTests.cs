using AssetTracker.Infrastructure.Repositories;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class DeviceRepositoryTests
{
    private readonly SqlServerFixture _fixture;

    public DeviceRepositoryTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private DeviceRepository CreateRepository() =>
        new(_fixture.ConnectionString, _fixture.CreateDbContext());

    [Fact]
    public async Task RegisterAsync_ThenGetByApiKeyHash_ReturnsSameDevice()
    {
        var repository = CreateRepository();
        var deviceId = $"test-device-{Guid.NewGuid():N}";
        var apiKeyHash = new byte[32];
        Random.Shared.NextBytes(apiKeyHash);

        var registered = await repository.RegisterAsync(deviceId, apiKeyHash, "Test Device", CancellationToken.None);
        var fetched = await repository.GetByApiKeyHashAsync(apiKeyHash, CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal(registered.Id, fetched!.Id);
        Assert.Equal(deviceId, fetched.DeviceId);
        Assert.Equal("Test Device", fetched.DisplayName);
        Assert.True(fetched.IsActive);
    }

    [Fact]
    public async Task GetByApiKeyHash_WithUnknownHash_ReturnsNull()
    {
        var repository = CreateRepository();
        var unknownHash = new byte[32];
        Random.Shared.NextBytes(unknownHash);

        var result = await repository.GetByApiKeyHashAsync(unknownHash, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByDeviceIdAsync_AfterRegister_ReturnsDevice()
    {
        var repository = CreateRepository();
        var deviceId = $"test-device-{Guid.NewGuid():N}";
        var apiKeyHash = new byte[32];
        Random.Shared.NextBytes(apiKeyHash);
        await repository.RegisterAsync(deviceId, apiKeyHash, null, CancellationToken.None);

        var result = await repository.GetByDeviceIdAsync(deviceId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(deviceId, result!.DeviceId);
    }

    [Fact]
    public async Task GetByDeviceIdAsync_WithUnknownDeviceId_ReturnsNull()
    {
        var repository = CreateRepository();

        var result = await repository.GetByDeviceIdAsync("does-not-exist", CancellationToken.None);

        Assert.Null(result);
    }
}
