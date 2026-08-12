using System.ComponentModel.DataAnnotations;

namespace AssetTracker.Application.Dtos;

public class LocationCreateDto
{
    [Required]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset Timestamp { get; set; }

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    public double? Altitude { get; set; }
    public double? Speed { get; set; }
    public byte? Satellites { get; set; }
    public double? Hdop { get; set; }
    public double? BatteryVoltage { get; set; }
    public bool IsStale { get; set; }
}
