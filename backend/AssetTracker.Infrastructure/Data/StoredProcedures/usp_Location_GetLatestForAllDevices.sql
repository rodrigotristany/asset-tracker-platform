CREATE OR ALTER PROCEDURE usp_Location_GetLatestForAllDevices
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH RankedLocations AS (
        SELECT
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
            l.created_at AS CreatedAt,
            ROW_NUMBER() OVER (PARTITION BY l.device_fk ORDER BY l.[timestamp] DESC, l.id DESC) AS RowNum
        FROM locations l
    )
    SELECT
        r.Id,
        r.DeviceFk,
        d.device_id AS DeviceId,
        r.Timestamp,
        r.Latitude,
        r.Longitude,
        r.Altitude,
        r.Speed,
        r.Satellites,
        r.Hdop,
        r.BatteryVoltage,
        r.IsStale,
        r.CreatedAt
    FROM RankedLocations r
    INNER JOIN devices d ON d.id = r.DeviceFk
    WHERE r.RowNum = 1
    ORDER BY r.Timestamp DESC;
END
