using AssetTracker.Infrastructure.Repositories;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class AdminUserRepositoryTests
{
    private readonly SqlServerFixture _fixture;

    public AdminUserRepositoryTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetByUsernameAsync_WithSeededAdmin_ReturnsUserWithValidPasswordHash()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new AdminUserRepository(dbContext);

        var user = await repository.GetByUsernameAsync("admin", CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal("admin", user!.Username);
        Assert.True(BCrypt.Net.BCrypt.Verify("ChangeMe123!", user.PasswordHash));
    }

    [Fact]
    public async Task GetByUsernameAsync_WithUnknownUsername_ReturnsNull()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new AdminUserRepository(dbContext);

        var user = await repository.GetByUsernameAsync("does-not-exist", CancellationToken.None);

        Assert.Null(user);
    }
}
