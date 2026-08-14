using AssetTracker.Application.Dtos;
using AssetTracker.Application.Exceptions;
using AssetTracker.Application.Interfaces;
using AssetTracker.Application.Services;
using AssetTracker.Domain.Entities;
using Moq;
using Xunit;

namespace AssetTracker.Tests.Unit.Services;

public class LocationServiceTests
{
    private readonly Mock<ILocationRepository> _locationRepository = new();
    private readonly Mock<IDeviceRepository> _deviceRepository = new();
    private readonly LocationService _sut;

    public LocationServiceTests()
    {
        _sut = new LocationService(_locationRepository.Object, _deviceRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownDevice_ThrowsDeviceNotFoundException()
    {
        _deviceRepository.Setup(r => r.GetByDeviceIdAsync("ghost-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Device?)null);

        var request = new LocationCreateDto { DeviceId = "ghost-001", Timestamp = DateTimeOffset.UtcNow, Latitude = 1, Longitude = 1 };

        await Assert.ThrowsAsync<DeviceNotFoundException>(() => _sut.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WithKnownDevice_ReturnsAcceptedResponse()
    {
        var device = Device.Reconstitute(5, "goat-001", new byte[] { 1 }, null, true, DateTime.UtcNow);
        _deviceRepository.Setup(r => r.GetByDeviceIdAsync("goat-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var savedLocation = Location.Reconstitute(100, 5, DateTimeOffset.UtcNow, 1, 1, null, null, null, null, null, false, DateTime.UtcNow);
        _locationRepository.Setup(r => r.InsertAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedLocation);

        var request = new LocationCreateDto { DeviceId = "goat-001", Timestamp = DateTimeOffset.UtcNow, Latitude = 1, Longitude = 1 };

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        Assert.Equal(100, result.Id);
        Assert.Equal("accepted", result.Status);
    }

    [Fact]
    public async Task GetLatestByDeviceAsync_ReturnsMappedDto()
    {
        var location = Location.Reconstitute(1, 5, DateTimeOffset.UtcNow, 10, 20, null, null, null, null, null, true, DateTime.UtcNow);
        _locationRepository.Setup(r => r.GetLatestByDeviceAsync("goat-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        var result = await _sut.GetLatestByDeviceAsync("goat-001", CancellationToken.None);

        Assert.Equal("goat-001", result.DeviceId);
        Assert.True(result.IsStale);
    }

    [Fact]
    public async Task GetLatestByDeviceAsync_WithNoRecordedLocation_ThrowsLocationNotFoundException()
    {
        _locationRepository.Setup(r => r.GetLatestByDeviceAsync("goat-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);

        await Assert.ThrowsAsync<LocationNotFoundException>(() => _sut.GetLatestByDeviceAsync("goat-001", CancellationToken.None));
    }
}
