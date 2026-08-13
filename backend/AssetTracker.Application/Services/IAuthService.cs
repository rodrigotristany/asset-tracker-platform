using AssetTracker.Application.Dtos;

namespace AssetTracker.Application.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct);
}
