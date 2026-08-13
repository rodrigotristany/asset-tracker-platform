using System.ComponentModel.DataAnnotations;
using AssetTracker.Application.Dtos;
using Xunit;

namespace AssetTracker.Tests.Unit.Dtos;

public class LoginRequestDtoValidationTests
{
    private static IList<ValidationResult> Validate(LoginRequestDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Validate_WithEmptyUsername_ReturnsError()
    {
        var dto = new LoginRequestDto { Username = "", Password = "something" };

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(LoginRequestDto.Username)));
    }

    [Fact]
    public void Validate_WithEmptyPassword_ReturnsError()
    {
        var dto = new LoginRequestDto { Username = "admin", Password = "" };

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(LoginRequestDto.Password)));
    }

    [Fact]
    public void Validate_WithBothFieldsSet_ReturnsNoErrors()
    {
        var dto = new LoginRequestDto { Username = "admin", Password = "something" };

        Assert.Empty(Validate(dto));
    }
}
