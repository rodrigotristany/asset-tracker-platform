using System.ComponentModel.DataAnnotations;
using AssetTracker.Application.Dtos;
using Xunit;

namespace AssetTracker.Tests.Unit.Dtos;

public class DeviceRegisterRequestDtoValidationTests
{
    private static IList<ValidationResult> Validate(DeviceRegisterRequestDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Validate_WithEmptyDeviceId_ReturnsError()
    {
        var dto = new DeviceRegisterRequestDto { DeviceId = "" };

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(DeviceRegisterRequestDto.DeviceId)));
    }

    [Fact]
    public void Validate_WithDeviceIdAndNoDisplayName_ReturnsNoErrors()
    {
        var dto = new DeviceRegisterRequestDto { DeviceId = "goat-001" };

        Assert.Empty(Validate(dto));
    }
}
