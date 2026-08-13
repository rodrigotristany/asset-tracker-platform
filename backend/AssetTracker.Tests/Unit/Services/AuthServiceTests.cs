using AssetTracker.Application.Dtos;
using AssetTracker.Application.Exceptions;
using AssetTracker.Application.Interfaces;
using AssetTracker.Application.Services;
using AssetTracker.Domain.Entities;
using Moq;
using Xunit;

namespace AssetTracker.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<IAdminUserRepository> _adminUserRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_adminUserRepository.Object, _passwordHasher.Object, _jwtTokenGenerator.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        var user = new AdminUser("admin", "hashed-password");
        _adminUserRepository.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("correct-password", "hashed-password")).Returns(true);
        _jwtTokenGenerator.Setup(g => g.GenerateToken(user)).Returns("fake-jwt-token");

        var result = await _sut.LoginAsync(new LoginRequestDto { Username = "admin", Password = "correct-password" }, CancellationToken.None);

        Assert.Equal("fake-jwt-token", result.Token);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownUsername_ThrowsInvalidCredentialsException()
    {
        _adminUserRepository.Setup(r => r.GetByUsernameAsync("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Username = "ghost", Password = "whatever" }, CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_WithUnknownUsername_StillInvokesPasswordHasher()
    {
        // Guards against a timing side-channel: if Verify is skipped for unknown usernames,
        // login responses for "unknown user" and "known user, wrong password" complete at
        // different speeds, letting an attacker enumerate valid usernames.
        _adminUserRepository.Setup(r => r.GetByUsernameAsync("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Username = "ghost", Password = "whatever" }, CancellationToken.None));

        _passwordHasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentialsException()
    {
        var user = new AdminUser("admin", "hashed-password");
        _adminUserRepository.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("wrong-password", "hashed-password")).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Username = "admin", Password = "wrong-password" }, CancellationToken.None));
    }
}
