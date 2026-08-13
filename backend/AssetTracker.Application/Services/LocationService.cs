using AssetTracker.Application.Dtos;
using AssetTracker.Application.Exceptions;
using AssetTracker.Application.Interfaces;
using AssetTracker.Domain.Entities;

namespace AssetTracker.Application.Services;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _locationRepository;
    private readonly IDeviceRepository _deviceRepository;

    public LocationService(ILocationRepository locationRepository, IDeviceRepository deviceRepository)
    {
        _locationRepository = locationRepository;
        _deviceRepository = deviceRepository;
    }

    public async Task<LocationCreateResponseDto> CreateAsync(LocationCreateDto request, CancellationToken ct)
    {
        var device = await _deviceRepository.GetByDeviceIdAsync(request.DeviceId, ct)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        var location = new Location(
            device.Id, request.Timestamp, request.Latitude, request.Longitude,
            request.Altitude, request.Speed, request.Satellites, request.Hdop,
            request.BatteryVoltage, request.IsStale);

        var saved = await _locationRepository.InsertAsync(location, ct);

        return new LocationCreateResponseDto { Id = saved.Id, Status = "accepted" };
    }

    public async Task<IReadOnlyList<LocationCreateResponseDto>> CreateBatchAsync(LocationBatchCreateDto request, CancellationToken ct)
    {
        var device = await _deviceRepository.GetByDeviceIdAsync(request.DeviceId, ct)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        var locations = request.Locations
            .Select(l => new Location(
                device.Id, l.Timestamp, l.Latitude, l.Longitude,
                l.Altitude, l.Speed, l.Satellites, l.Hdop, l.BatteryVoltage, l.IsStale))
            .ToList();

        var saved = await _locationRepository.BatchInsertAsync(locations, ct);

        return saved.Select(l => new LocationCreateResponseDto { Id = l.Id, Status = "accepted" }).ToList();
    }

    public async Task<IReadOnlyList<LocationReadDto>> GetLatestByDeviceAsync(string deviceId, CancellationToken ct)
    {
        var locations = await _locationRepository.GetLatestByDeviceAsync(deviceId, ct);

        return locations.Select(l => new LocationReadDto
        {
            Id = l.Id,
            DeviceId = deviceId,
            Timestamp = l.Timestamp,
            Latitude = l.Latitude,
            Longitude = l.Longitude,
            Altitude = l.Altitude,
            Speed = l.Speed,
            Satellites = l.Satellites,
            Hdop = l.Hdop,
            BatteryVoltage = l.BatteryVoltage,
            IsStale = l.IsStale
        }).ToList();
    }
}
