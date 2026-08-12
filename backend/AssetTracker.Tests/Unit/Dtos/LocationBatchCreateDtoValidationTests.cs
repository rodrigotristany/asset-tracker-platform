using System.ComponentModel.DataAnnotations;
using AssetTracker.Application.Dtos;
using Xunit;

namespace AssetTracker.Tests.Unit.Dtos;

public class LocationBatchCreateDtoValidationTests
{
    private static IList<ValidationResult> Validate(LocationBatchCreateDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Validate_WithEmptyLocationsList_ReturnsError()
    {
        var dto = new LocationBatchCreateDto { DeviceId = "goat-001", Locations = new List<LocationCreateDto>() };

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(LocationBatchCreateDto.Locations)));
    }

    [Fact]
    public void Validate_WithAtLeastOneLocation_ReturnsNoErrors()
    {
        var dto = new LocationBatchCreateDto
        {
            DeviceId = "goat-001",
            Locations = new List<LocationCreateDto>
            {
                new() { DeviceId = "goat-001", Timestamp = DateTimeOffset.UtcNow, Latitude = 1, Longitude = 1 }
            }
        };

        Assert.Empty(Validate(dto));
    }
}
