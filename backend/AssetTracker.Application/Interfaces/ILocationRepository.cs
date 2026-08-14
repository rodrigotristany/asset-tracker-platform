using AssetTracker.Domain.Entities;

namespace AssetTracker.Application.Interfaces;

public interface ILocationRepository
{
    Task<Location> InsertAsync(Location location, CancellationToken ct);
    Task<IReadOnlyList<Location>> BatchInsertAsync(IReadOnlyList<Location> locations, CancellationToken ct);
    Task<Location?> GetLatestByDeviceAsync(string deviceId, CancellationToken ct);
    Task<IReadOnlyList<(string DeviceId, Location Location)>> GetLatestForAllDevicesAsync(CancellationToken ct);
}
