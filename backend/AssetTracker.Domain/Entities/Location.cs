namespace AssetTracker.Domain.Entities;

public class Location
{
    public long Id { get; private set; }
    public int DeviceFk { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double? Altitude { get; private set; }
    public double? Speed { get; private set; }
    public byte? Satellites { get; private set; }
    public double? Hdop { get; private set; }
    public double? BatteryVoltage { get; private set; }
    public bool IsStale { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Location() { }

    public Location(
        int deviceFk,
        DateTimeOffset timestamp,
        double latitude,
        double longitude,
        double? altitude,
        double? speed,
        byte? satellites,
        double? hdop,
        double? batteryVoltage,
        bool isStale)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

        DeviceFk = deviceFk;
        Timestamp = timestamp;
        Latitude = latitude;
        Longitude = longitude;
        Altitude = altitude;
        Speed = speed;
        Satellites = satellites;
        Hdop = hdop;
        BatteryVoltage = batteryVoltage;
        IsStale = isStale;
        CreatedAt = DateTime.UtcNow;
    }

    public static Location Reconstitute(
        long id,
        int deviceFk,
        DateTimeOffset timestamp,
        double latitude,
        double longitude,
        double? altitude,
        double? speed,
        byte? satellites,
        double? hdop,
        double? batteryVoltage,
        bool isStale,
        DateTime createdAt)
    {
        return new Location
        {
            Id = id,
            DeviceFk = deviceFk,
            Timestamp = timestamp,
            Latitude = latitude,
            Longitude = longitude,
            Altitude = altitude,
            Speed = speed,
            Satellites = satellites,
            Hdop = hdop,
            BatteryVoltage = batteryVoltage,
            IsStale = isStale,
            CreatedAt = createdAt
        };
    }
}
