CREATE OR ALTER PROCEDURE usp_Location_GetLatestByDevice
    @DeviceId NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        l.id AS Id,
        l.device_fk AS DeviceFk,
        l.[timestamp] AS Timestamp,
        l.latitude AS Latitude,
        l.longitude AS Longitude,
        l.altitude AS Altitude,
        l.speed AS Speed,
        l.satellites AS Satellites,
        l.hdop AS Hdop,
        l.battery_voltage AS BatteryVoltage,
        l.is_stale AS IsStale,
        l.created_at AS CreatedAt
    FROM locations l
    INNER JOIN devices d ON d.id = l.device_fk
    WHERE d.device_id = @DeviceId
    ORDER BY l.[timestamp] DESC;
END
