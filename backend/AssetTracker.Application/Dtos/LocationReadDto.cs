namespace AssetTracker.Application.Dtos;

public class LocationReadDto
{
    public long Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Altitude { get; set; }
    public double? Speed { get; set; }
    public byte? Satellites { get; set; }
    public double? Hdop { get; set; }
    public double? BatteryVoltage { get; set; }
    public bool IsStale { get; set; }
}
