using AssetTracker.Api.Auth;
using AssetTracker.Application.Dtos;
using AssetTracker.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetTracker.Api.Controllers;

[ApiController]
[Route("api/v1/devices")]
[Authorize(AuthenticationSchemes = AuthSchemes.Jwt)]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly ILocationService _locationService;

    public DevicesController(IDeviceService deviceService, ILocationService locationService)
    {
        _deviceService = deviceService;
        _locationService = locationService;
    }

    [HttpPost]
    public async Task<ActionResult<DeviceRegisterResponseDto>> Register([FromBody] DeviceRegisterRequestDto request, CancellationToken ct)
    {
        var response = await _deviceService.RegisterAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LocationReadDto>>> GetAllLatestLocations(CancellationToken ct)
    {
        var response = await _locationService.GetAllLatestLocationsAsync(ct);
        return Ok(response);
    }
}
