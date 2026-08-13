using System.ComponentModel.DataAnnotations;

namespace AssetTracker.Application.Dtos;

public class LocationBatchCreateDto
{
    [Required]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "At least one location is required.")]
    public List<LocationCreateDto> Locations { get; set; } = new();
}
