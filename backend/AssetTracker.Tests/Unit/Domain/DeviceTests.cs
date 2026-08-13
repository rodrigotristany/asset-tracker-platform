using AssetTracker.Domain.Entities;
using Xunit;

namespace AssetTracker.Tests.Unit.Domain;

public class DeviceTests
{
    [Fact]
    public void Constructor_WithEmptyDeviceId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Device("", new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public void Constructor_WithEmptyApiKeyHash_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Device("goat-001", Array.Empty<byte>()));
    }

    [Fact]
    public void Constructor_WithValidArgs_SetsDefaults()
    {
        var device = new Device("goat-001", new byte[] { 1, 2, 3 }, "Goat 001");

        Assert.Equal("goat-001", device.DeviceId);
        Assert.Equal("Goat 001", device.DisplayName);
        Assert.True(device.IsActive);
    }

    [Fact]
    public void Reconstitute_PreservesAllFields()
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var hash = new byte[] { 9, 9, 9 };

        var device = Device.Reconstitute(42, "goat-002", hash, "Goat 002", false, createdAt);

        Assert.Equal(42, device.Id);
        Assert.Equal("goat-002", device.DeviceId);
        Assert.Equal(hash, device.ApiKeyHash);
        Assert.Equal("Goat 002", device.DisplayName);
        Assert.False(device.IsActive);
        Assert.Equal(createdAt, device.CreatedAt);
    }
}
