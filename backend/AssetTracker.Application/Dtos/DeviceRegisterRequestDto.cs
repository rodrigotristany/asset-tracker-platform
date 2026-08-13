using System.ComponentModel.DataAnnotations;

namespace AssetTracker.Application.Dtos;

public class DeviceRegisterRequestDto
{
    [Required]
    public string DeviceId { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}
