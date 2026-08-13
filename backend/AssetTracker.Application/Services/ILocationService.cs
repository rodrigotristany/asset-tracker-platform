using AssetTracker.Application.Dtos;

namespace AssetTracker.Application.Services;

public interface ILocationService
{
    Task<LocationCreateResponseDto> CreateAsync(LocationCreateDto request, CancellationToken ct);
    Task<IReadOnlyList<LocationCreateResponseDto>> CreateBatchAsync(LocationBatchCreateDto request, CancellationToken ct);
    Task<IReadOnlyList<LocationReadDto>> GetLatestByDeviceAsync(string deviceId, CancellationToken ct);
}
