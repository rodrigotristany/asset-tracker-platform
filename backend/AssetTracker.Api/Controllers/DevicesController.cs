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

    public DevicesController(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpPost]
    public async Task<ActionResult<DeviceRegisterResponseDto>> Register([FromBody] DeviceRegisterRequestDto request, CancellationToken ct)
    {
        var response = await _deviceService.RegisterAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
