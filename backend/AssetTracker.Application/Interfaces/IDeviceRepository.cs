using AssetTracker.Domain.Entities;

namespace AssetTracker.Application.Interfaces;

public interface IDeviceRepository
{
    Task<Device> RegisterAsync(string deviceId, byte[] apiKeyHash, string? displayName, CancellationToken ct);
    Task<Device?> GetByApiKeyHashAsync(byte[] apiKeyHash, CancellationToken ct);
    Task<Device?> GetByDeviceIdAsync(string deviceId, CancellationToken ct);
}
