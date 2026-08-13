using System.Security.Claims;
using AssetTracker.Api.Auth;
using AssetTracker.Application.Dtos;
using AssetTracker.Application.Exceptions;
using AssetTracker.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetTracker.Api.Controllers;

[ApiController]
[Route("api/v1/locations")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationsController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthSchemes.ApiKey)]
    public async Task<ActionResult<LocationCreateResponseDto>> Create([FromBody] LocationCreateDto request, CancellationToken ct)
    {
        EnsureDeviceOwnership(request.DeviceId);
        var response = await _locationService.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("batch")]
    [Authorize(AuthenticationSchemes = AuthSchemes.ApiKey)]
    public async Task<ActionResult<IReadOnlyList<LocationCreateResponseDto>>> CreateBatch([FromBody] LocationBatchCreateDto request, CancellationToken ct)
    {
        EnsureDeviceOwnership(request.DeviceId);

        // LocationService.CreateBatchAsync currently ignores each item's DeviceId and only
        // trusts the batch's top-level DeviceId - but the DTO still carries a per-item
        // DeviceId that DataAnnotations validates. If a future change starts reading it
        // (e.g. to support mixed-device batches), it must not silently reopen the same
        // forgery path the top-level check above closes. Verify every item here too, even
        // though the service doesn't need it today, so there's no discarded field that a
        // later refactor could quietly turn into an authorization bypass.
        foreach (var item in request.Locations)
        {
            EnsureDeviceOwnership(item.DeviceId);
        }

        var response = await _locationService.CreateBatchAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{deviceId}")]
    [Authorize(AuthenticationSchemes = AuthSchemes.Jwt)]
    public async Task<ActionResult<IReadOnlyList<LocationReadDto>>> GetLatestByDevice(string deviceId, CancellationToken ct)
    {
        var response = await _locationService.GetLatestByDeviceAsync(deviceId, ct);
        return Ok(response);
    }

    /// <summary>
    /// The ApiKey scheme authenticates the device via <see cref="ClaimTypes.NameIdentifier"/>
    /// (see ApiKeyAuthenticationHandler), but that alone does not stop the authenticated device
    /// from writing location data for a DIFFERENT device by putting a different deviceId in the
    /// request body. Enforce object-level authorization here: the authenticated device's
    /// identifier must match the deviceId the request is submitting data for. Called for both
    /// the top-level DeviceId and (in CreateBatch) each item's own DeviceId, so there is no
    /// deviceId anywhere in the request that goes unchecked.
    /// </summary>
    private void EnsureDeviceOwnership(string requestedDeviceId)
    {
        var authenticatedDeviceId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (authenticatedDeviceId is null || !string.Equals(authenticatedDeviceId, requestedDeviceId, StringComparison.Ordinal))
        {
            throw new DeviceOwnershipMismatchException(authenticatedDeviceId ?? "unknown", requestedDeviceId);
        }
    }
}
