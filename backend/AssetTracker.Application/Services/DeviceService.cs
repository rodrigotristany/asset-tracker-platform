using System.Security.Cryptography;
using AssetTracker.Application.Dtos;
using AssetTracker.Application.Exceptions;
using AssetTracker.Application.Interfaces;

namespace AssetTracker.Application.Services;

public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _deviceRepository;

    public DeviceService(IDeviceRepository deviceRepository)
    {
        _deviceRepository = deviceRepository;
    }

    public async Task<DeviceRegisterResponseDto> RegisterAsync(DeviceRegisterRequestDto request, CancellationToken ct)
    {
        var existing = await _deviceRepository.GetByDeviceIdAsync(request.DeviceId, ct);
        if (existing is not null)
            throw new DeviceAlreadyExistsException(request.DeviceId);

        var apiKeyBytes = RandomNumberGenerator.GetBytes(32);
        var apiKey = Convert.ToBase64String(apiKeyBytes);
        var apiKeyHash = SHA256.HashData(apiKeyBytes);

        await _deviceRepository.RegisterAsync(request.DeviceId, apiKeyHash, request.DisplayName, ct);

        return new DeviceRegisterResponseDto
        {
            DeviceId = request.DeviceId,
            ApiKey = apiKey
        };
    }
}
