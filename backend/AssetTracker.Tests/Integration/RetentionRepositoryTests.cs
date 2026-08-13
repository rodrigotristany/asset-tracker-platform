using AssetTracker.Domain.Entities;
using AssetTracker.Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
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

        // Prove this test's own rows exist before purging, scoped to this device.
        // The purge stored procedure operates across ALL devices in the shared
        // Testcontainers database, so the global deletedCount alone is not proof
        // about this test's rows - it could be inflated or masked by unrelated
        // tests inserting old-timestamp locations of their own.
        Assert.Equal(2, await CountLocationsForDeviceAsync(device.Id));

        var retentionRepository = new RetentionRepository(_fixture.ConnectionString);
        var deletedCount = await retentionRepository.PurgeOldLocationsAsync(30, CancellationToken.None);

        Assert.True(deletedCount >= 1);
        Assert.Equal(1, await CountLocationsForDeviceAsync(device.Id));

        var remaining = await locationRepository.GetLatestByDeviceAsync(device.DeviceId, CancellationToken.None);
        Assert.Single(remaining);
        Assert.Equal(savedRecent.Id, remaining[0].Id);
    }

    private async Task<int> CountLocationsForDeviceAsync(int deviceFk)
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM locations WHERE device_fk = @DeviceFk";
        command.Parameters.AddWithValue("@DeviceFk", deviceFk);

        return (int)(await command.ExecuteScalarAsync())!;
    }
}
