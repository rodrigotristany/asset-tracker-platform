using AssetTracker.Domain.Entities;
using AssetTracker.Infrastructure.Repositories;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class RetentionRepositoryTests
{
    private readonly SqlServerFixture _fixture;

    public RetentionRepositoryTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PurgeOldLocationsAsync_DeletesOnlyLocationsOlderThanRetentionWindow()
    {
        var deviceRepository = new DeviceRepository(_fixture.ConnectionString, _fixture.CreateDbContext());
        var apiKeyHash = new byte[32];
        Random.Shared.NextBytes(apiKeyHash);
        var device = await deviceRepository.RegisterAsync($"test-device-{Guid.NewGuid():N}", apiKeyHash, null, CancellationToken.None);

        var locationRepository = new LocationRepository(_fixture.ConnectionString);
        var oldLocation = new Location(device.Id, DateTimeOffset.UtcNow.AddDays(-40), 1, 1, null, null, null, null, null, false);
        var recentLocation = new Location(device.Id, DateTimeOffset.UtcNow.AddDays(-1), 2, 2, null, null, null, null, null, false);
        await locationRepository.InsertAsync(oldLocation, CancellationToken.None);
        var savedRecent = await locationRepository.InsertAsync(recentLocation, CancellationToken.None);

        var retentionRepository = new RetentionRepository(_fixture.ConnectionString);
        var deletedCount = await retentionRepository.PurgeOldLocationsAsync(30, CancellationToken.None);

        Assert.Equal(1, deletedCount);
        var remaining = await locationRepository.GetLatestByDeviceAsync(device.DeviceId, CancellationToken.None);
        Assert.Single(remaining);
        Assert.Equal(savedRecent.Id, remaining[0].Id);
    }
}
