using AssetTracker.Api.Auth;
using AssetTracker.Application.Dtos;
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
        var response = await _locationService.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("batch")]
    [Authorize(AuthenticationSchemes = AuthSchemes.ApiKey)]
    public async Task<ActionResult<IReadOnlyList<LocationCreateResponseDto>>> CreateBatch([FromBody] LocationBatchCreateDto request, CancellationToken ct)
    {
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
}
