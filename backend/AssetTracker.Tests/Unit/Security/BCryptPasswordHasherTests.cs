using AssetTracker.Infrastructure.Security;
using Xunit;

namespace AssetTracker.Tests.Unit.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("CorrectHorseBatteryStaple");

        Assert.True(_hasher.Verify("CorrectHorseBatteryStaple", hash));
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("CorrectHorseBatteryStaple");

        Assert.False(_hasher.Verify("WrongPassword", hash));
    }
}
