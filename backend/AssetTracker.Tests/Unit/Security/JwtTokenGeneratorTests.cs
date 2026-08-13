using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using AssetTracker.Domain.Entities;
using AssetTracker.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AssetTracker.Tests.Unit.Security;

public class JwtTokenGeneratorTests
{
    private static JwtOptions CreateOptions() => new()
    {
        Key = "a-test-signing-key-that-is-at-least-32-bytes-long",
        Issuer = "AssetTrackerApi",
        Audience = "AssetTrackerDashboard",
        ExpiryMinutes = 60
    };

    [Fact]
    public void GenerateToken_ReturnsThreePartToken_ForValidUser()
    {
        var options = Options.Create(CreateOptions());
        var generator = new JwtTokenGenerator(options);
        var user = new AdminUser("admin", "hashed-password");

        var token = generator.GenerateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public void GenerateToken_IncludesExpectedClaims_ForValidUser()
    {
        var jwtOptions = CreateOptions();
        var generator = new JwtTokenGenerator(Options.Create(jwtOptions));
        var user = new AdminUser("admin", "hashed-password");

        var token = generator.GenerateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("admin", jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.True(Guid.TryParse(jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value, out _));
        Assert.Equal(jwtOptions.Issuer, jwt.Issuer);
        Assert.Contains(jwtOptions.Audience, jwt.Audiences);
        Assert.True(jwt.ValidTo > DateTime.UtcNow);
        Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(jwtOptions.ExpiryMinutes).AddSeconds(5));
    }

    [Fact]
    public void GenerateToken_ProducesTokenThatValidatesAgainstSigningKey()
    {
        var jwtOptions = CreateOptions();
        var generator = new JwtTokenGenerator(Options.Create(jwtOptions));
        var user = new AdminUser("admin", "hashed-password");

        var token = generator.GenerateToken(user);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5)
        };

        // MapInboundClaims disabled so validated claim types match the JWT's own claim names
        // (JwtSecurityTokenHandler otherwise remaps "sub" to the legacy ClaimTypes.NameIdentifier URI).
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

        Assert.NotNull(validatedToken);
        Assert.Equal("admin", principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
    }

    [Fact]
    public void GenerateToken_WithWrongSigningKey_FailsValidation()
    {
        var jwtOptions = CreateOptions();
        var generator = new JwtTokenGenerator(Options.Create(jwtOptions));
        var user = new AdminUser("admin", "hashed-password");

        var token = generator.GenerateToken(user);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-completely-different-signing-key-at-least-32-bytes")),
            ValidateLifetime = true
        };

        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _));
    }
}
