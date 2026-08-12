namespace AssetTracker.Domain.Entities;

public class Device
{
    public int Id { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public byte[] ApiKeyHash { get; private set; } = Array.Empty<byte>();
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Device() { }

    public Device(string deviceId, byte[] apiKeyHash, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID is required.", nameof(deviceId));
        if (apiKeyHash is null || apiKeyHash.Length == 0)
            throw new ArgumentException("API key hash is required.", nameof(apiKeyHash));

        DeviceId = deviceId;
        ApiKeyHash = apiKeyHash;
        DisplayName = displayName;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Device Reconstitute(int id, string deviceId, byte[] apiKeyHash, string? displayName, bool isActive, DateTime createdAt)
    {
        return new Device
        {
            Id = id,
            DeviceId = deviceId,
            ApiKeyHash = apiKeyHash,
            DisplayName = displayName,
            IsActive = isActive,
            CreatedAt = createdAt
        };
    }
}
