using AssetTracker.Application.Dtos;
using AssetTracker.Application.Exceptions;
using AssetTracker.Application.Interfaces;

namespace AssetTracker.Application.Services;

public class AuthService : IAuthService
{
    // A syntactically valid BCrypt hash with no corresponding real password or user — used only
    // to equalize the cost of IPasswordHasher.Verify when the looked-up user doesn't exist, so
    // login timing doesn't leak whether a username is registered.
    private const string DummyPasswordHashForTimingSafety =
        "$2a$11$q.dnnSWAQy0.edkeQLU8KesCJxsUwP9Bc9LKJhLf9u9Eo83p75SoW";

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

        var passwordHashToCheck = user?.PasswordHash ?? DummyPasswordHashForTimingSafety;
        var isPasswordValid = _passwordHasher.Verify(request.Password, passwordHashToCheck);

        if (user is null || !isPasswordValid)
            throw new InvalidCredentialsException();

        var token = _jwtTokenGenerator.GenerateToken(user);
        return new LoginResponseDto { Token = token };
    }
}
