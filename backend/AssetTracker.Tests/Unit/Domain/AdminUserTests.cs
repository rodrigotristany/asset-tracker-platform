using AssetTracker.Domain.Entities;
using Xunit;

namespace AssetTracker.Tests.Unit.Domain;

public class AdminUserTests
{
    [Fact]
    public void Constructor_WithEmptyUsername_Throws()
    {
        Assert.Throws<ArgumentException>(() => new AdminUser("", "hash"));
    }

    [Fact]
    public void Constructor_WithEmptyPasswordHash_Throws()
    {
        Assert.Throws<ArgumentException>(() => new AdminUser("admin", ""));
    }

    [Fact]
    public void Constructor_WithValidArgs_SetsFields()
    {
        var user = new AdminUser("admin", "$2a$11$hashedvalue");

        Assert.Equal("admin", user.Username);
        Assert.Equal("$2a$11$hashedvalue", user.PasswordHash);
    }
}
