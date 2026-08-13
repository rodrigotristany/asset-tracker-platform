using AssetTracker.Application.Dtos;
using AssetTracker.Application.Exceptions;
using AssetTracker.Application.Interfaces;

namespace AssetTracker.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAdminUserRepository _adminUserRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IAdminUserRepository adminUserRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _adminUserRepository = adminUserRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct)
    {
        var user = await _adminUserRepository.GetByUsernameAsync(request.Username, ct);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        var token = _jwtTokenGenerator.GenerateToken(user);
        return new LoginResponseDto { Token = token };
    }
}
