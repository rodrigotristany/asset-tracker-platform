using AssetTracker.Domain.Entities;
using Xunit;

namespace AssetTracker.Tests.Unit.Domain;

public class LocationTests
{
    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Constructor_WithInvalidLatitude_Throws(double latitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Location(1, DateTimeOffset.UtcNow, latitude, 0, null, null, null, null, null, false));
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Constructor_WithInvalidLongitude_Throws(double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Location(1, DateTimeOffset.UtcNow, 0, longitude, null, null, null, null, null, false));
    }

    [Fact]
    public void Constructor_WithValidArgs_SetsFields()
    {
        var timestamp = DateTimeOffset.UtcNow;

        var location = new Location(7, timestamp, -31.4231, -62.0834, 142.1, 0.4, 9, 0.8, 3.7, false);

        Assert.Equal(7, location.DeviceFk);
        Assert.Equal(timestamp, location.Timestamp);
        Assert.Equal(-31.4231, location.Latitude);
        Assert.False(location.IsStale);
    }

    [Fact]
    public void Reconstitute_PreservesAllFields()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var createdAt = DateTime.UtcNow;

        var location = Location.Reconstitute(100, 7, timestamp, 1.1, 2.2, 3.3, 4.4, 5, 6.6, 7.7, true, createdAt);

        Assert.Equal(100, location.Id);
        Assert.Equal(7, location.DeviceFk);
        Assert.True(location.IsStale);
        Assert.Equal(createdAt, location.CreatedAt);
    }
}
