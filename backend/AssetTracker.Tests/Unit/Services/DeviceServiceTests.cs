using System.Security.Cryptography;
using AssetTracker.Application.Dtos;
using AssetTracker.Application.Exceptions;
using AssetTracker.Application.Interfaces;
using AssetTracker.Application.Services;
using AssetTracker.Domain.Entities;
using Moq;
using Xunit;

namespace AssetTracker.Tests.Unit.Services;

public class DeviceServiceTests
{
    private readonly Mock<IDeviceRepository> _deviceRepository = new();
    private readonly DeviceService _sut;

    public DeviceServiceTests()
    {
        _sut = new DeviceService(_deviceRepository.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithNewDeviceId_ReturnsApiKey()
    {
        _deviceRepository.Setup(r => r.GetByDeviceIdAsync("goat-003", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Device?)null);
        _deviceRepository
            .Setup(r => r.RegisterAsync("goat-003", It.IsAny<byte[]>(), "Goat 3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Device.Reconstitute(1, "goat-003", new byte[] { 1 }, "Goat 3", true, DateTime.UtcNow));

        var result = await _sut.RegisterAsync(new DeviceRegisterRequestDto { DeviceId = "goat-003", DisplayName = "Goat 3" }, CancellationToken.None);

        Assert.Equal("goat-003", result.DeviceId);
        Assert.False(string.IsNullOrWhiteSpace(result.ApiKey));
    }

    [Fact]
    public async Task RegisterAsync_WithNewDeviceId_ApiKeyRoundTripsToStoredHash()
    {
        byte[]? capturedHash = null;

        _deviceRepository.Setup(r => r.GetByDeviceIdAsync("goat-004", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Device?)null);
        _deviceRepository
            .Setup(r => r.RegisterAsync("goat-004", It.IsAny<byte[]>(), "Goat 4", It.IsAny<CancellationToken>()))
            .Callback<string, byte[], string?, CancellationToken>((_, apiKeyHash, _, _) => capturedHash = apiKeyHash)
            .ReturnsAsync(Device.Reconstitute(1, "goat-004", new byte[] { 1 }, "Goat 4", true, DateTime.UtcNow));

        var result = await _sut.RegisterAsync(new DeviceRegisterRequestDto { DeviceId = "goat-004", DisplayName = "Goat 4" }, CancellationToken.None);

        Assert.NotNull(capturedHash);
        var decodedKey = Convert.FromBase64String(result.ApiKey);
        var computedHash = SHA256.HashData(decodedKey);

        Assert.Equal(capturedHash, computedHash);
        Assert.Equal(32, decodedKey.Length);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingDeviceId_ThrowsDeviceAlreadyExistsException()
    {
        _deviceRepository.Setup(r => r.GetByDeviceIdAsync("goat-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Device.Reconstitute(1, "goat-001", new byte[] { 1 }, null, true, DateTime.UtcNow));

        await Assert.ThrowsAsync<DeviceAlreadyExistsException>(() =>
            _sut.RegisterAsync(new DeviceRegisterRequestDto { DeviceId = "goat-001" }, CancellationToken.None));
    }
}
