using System.ComponentModel.DataAnnotations;
using AssetTracker.Application.Dtos;
using Xunit;

namespace AssetTracker.Tests.Unit.Dtos;

public class LocationCreateDtoValidationTests
{
    private static IList<ValidationResult> Validate(LocationCreateDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    private static LocationCreateDto ValidDto() => new()
    {
        DeviceId = "goat-001",
        Timestamp = DateTimeOffset.UtcNow,
        Latitude = -31.4231,
        Longitude = -62.0834
    };

    [Fact]
    public void Validate_WithValidDto_ReturnsNoErrors()
    {
        Assert.Empty(Validate(ValidDto()));
    }

    [Fact]
    public void Validate_WithEmptyDeviceId_ReturnsError()
    {
        var dto = ValidDto();
        dto.DeviceId = "";

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(LocationCreateDto.DeviceId)));
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Validate_WithLatitudeOutOfRange_ReturnsError(double latitude)
    {
        var dto = ValidDto();
        dto.Latitude = latitude;

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(LocationCreateDto.Latitude)));
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Validate_WithLongitudeOutOfRange_ReturnsError(double longitude)
    {
        var dto = ValidDto();
        dto.Longitude = longitude;

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(LocationCreateDto.Longitude)));
    }

    [Fact]
    public void Validate_WithOptionalFieldsNull_ReturnsNoErrors()
    {
        var dto = ValidDto();
        dto.Altitude = null;
        dto.Speed = null;
        dto.Satellites = null;
        dto.Hdop = null;
        dto.BatteryVoltage = null;

        Assert.Empty(Validate(dto));
    }
}
