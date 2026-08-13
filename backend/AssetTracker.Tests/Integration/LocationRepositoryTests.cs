using AssetTracker.Domain.Entities;
using AssetTracker.Infrastructure.Repositories;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class LocationRepositoryTests
{
    private readonly SqlServerFixture _fixture;

    public LocationRepositoryTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Device> RegisterDeviceAsync()
    {
        var deviceRepository = new DeviceRepository(_fixture.ConnectionString, _fixture.CreateDbContext());
        var apiKeyHash = new byte[32];
        Random.Shared.NextBytes(apiKeyHash);
        return await deviceRepository.RegisterAsync($"test-device-{Guid.NewGuid():N}", apiKeyHash, null, CancellationToken.None);
    }

    [Fact]
    public async Task InsertAsync_ReturnsPersistedLocationWithId()
    {
        var device = await RegisterDeviceAsync();
        var repository = new LocationRepository(_fixture.ConnectionString);
        var location = new Location(device.Id, DateTimeOffset.UtcNow, 1.5, 2.5, 10, 0.5, 8, 0.9, 3.9, false);

        var saved = await repository.InsertAsync(location, CancellationToken.None);

        Assert.True(saved.Id > 0);
        Assert.Equal(device.Id, saved.DeviceFk);
        Assert.Equal(1.5, saved.Latitude);
    }

    [Fact]
    public async Task BatchInsertAsync_InsertsAllRowsForSameDevice()
    {
        var device = await RegisterDeviceAsync();
        var repository = new LocationRepository(_fixture.ConnectionString);
        var locations = new List<Location>
        {
            new(device.Id, DateTimeOffset.UtcNow.AddMinutes(-2), 1, 1, null, null, null, null, null, false),
            new(device.Id, DateTimeOffset.UtcNow.AddMinutes(-1), 2, 2, null, null, null, null, null, false),
            new(device.Id, DateTimeOffset.UtcNow, 3, 3, null, null, null, null, null, true)
        };

        var saved = await repository.BatchInsertAsync(locations, CancellationToken.None);

        Assert.Equal(3, saved.Count);
        Assert.All(saved, l => Assert.Equal(device.Id, l.DeviceFk));
        Assert.All(saved, l => Assert.True(l.Id > 0));
    }

    // The DataTable that feeds LocationTableType binds columns POSITIONALLY, not by name (unlike
    // the Dapper named-parameter binding used by InsertAsync). Six of the nine columns are all
    // FLOAT (latitude, longitude, altitude, speed, hdop, battery_voltage), so a pairwise column
    // transposition in LocationRepository.BatchInsertAsync or in LocationTableType.sql's declared
    // column order would be silently type-compatible and undetectable by SQL Server. This test
    // uses distinct, non-null values in every field of every row (never symmetric like (1,1)/(2,2))
    // and asserts every field individually, so a transposed pair of columns produces a value
    // mismatch instead of passing unnoticed.
    [Fact]
    public async Task BatchInsertAsync_PersistsEveryFieldInDeclaredColumnOrder()
    {
        var device = await RegisterDeviceAsync();
        var repository = new LocationRepository(_fixture.ConnectionString);
        var now = DateTimeOffset.UtcNow;
        var locations = new List<Location>
        {
            new(device.Id, now.AddMinutes(-2), 10.111, -20.222, 100.5, 5.25, 7, 1.1, 3.71, false),
            new(device.Id, now.AddMinutes(-1), 20.222, -30.333, 200.5, 15.5, 9, 2.2, 3.82, true),
            new(device.Id, now, 30.333, -40.444, 300.5, 25.75, 11, 3.3, 3.93, false)
        };

        var saved = await repository.BatchInsertAsync(locations, CancellationToken.None);

        Assert.Equal(locations.Count, saved.Count);
        var orderedExpected = locations.OrderBy(l => l.Timestamp).ToList();
        var orderedActual = saved.OrderBy(l => l.Timestamp).ToList();

        for (var i = 0; i < orderedExpected.Count; i++)
        {
            var expected = orderedExpected[i];
            var actual = orderedActual[i];

            Assert.Equal(device.Id, actual.DeviceFk);
            Assert.Equal(expected.Timestamp, actual.Timestamp);
            Assert.Equal(expected.Latitude, actual.Latitude);
            Assert.Equal(expected.Longitude, actual.Longitude);
            Assert.Equal(expected.Altitude, actual.Altitude);
            Assert.Equal(expected.Speed, actual.Speed);
            Assert.Equal(expected.Satellites, actual.Satellites);
            Assert.Equal(expected.Hdop, actual.Hdop);
            Assert.Equal(expected.BatteryVoltage, actual.BatteryVoltage);
            Assert.Equal(expected.IsStale, actual.IsStale);
        }
    }

    [Fact]
    public async Task GetLatestByDeviceAsync_ReturnsOnlyMostRecentLocation()
    {
        var device = await RegisterDeviceAsync();
        var repository = new LocationRepository(_fixture.ConnectionString);
        await repository.InsertAsync(new Location(device.Id, DateTimeOffset.UtcNow.AddMinutes(-5), 1, 1, null, null, null, null, null, false), CancellationToken.None);
        var newest = await repository.InsertAsync(new Location(device.Id, DateTimeOffset.UtcNow, 9, 9, null, null, null, null, null, false), CancellationToken.None);

        var result = await repository.GetLatestByDeviceAsync(device.DeviceId, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(newest.Id, result[0].Id);
    }

    [Fact]
    public async Task GetLatestByDeviceAsync_WithNoLocations_ReturnsEmptyList()
    {
        var device = await RegisterDeviceAsync();
        var repository = new LocationRepository(_fixture.ConnectionString);

        var result = await repository.GetLatestByDeviceAsync(device.DeviceId, CancellationToken.None);

        Assert.Empty(result);
    }
}
