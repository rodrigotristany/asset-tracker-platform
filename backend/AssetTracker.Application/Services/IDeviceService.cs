using AssetTracker.Application.Dtos;

namespace AssetTracker.Application.Services;

public interface IDeviceService
{
    Task<DeviceRegisterResponseDto> RegisterAsync(DeviceRegisterRequestDto request, CancellationToken ct);
}
