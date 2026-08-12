CREATE OR ALTER PROCEDURE usp_Location_BatchInsert
    @DeviceFk INT,
    @Locations LocationTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO locations
        (device_fk, [timestamp], latitude, longitude, altitude, speed, satellites, hdop, battery_voltage, is_stale, created_at)
    OUTPUT
        INSERTED.id AS Id,
        INSERTED.device_fk AS DeviceFk,
        INSERTED.[timestamp] AS Timestamp,
        INSERTED.latitude AS Latitude,
        INSERTED.longitude AS Longitude,
        INSERTED.altitude AS Altitude,
        INSERTED.speed AS Speed,
        INSERTED.satellites AS Satellites,
        INSERTED.hdop AS Hdop,
        INSERTED.battery_voltage AS BatteryVoltage,
        INSERTED.is_stale AS IsStale,
        INSERTED.created_at AS CreatedAt
    SELECT @DeviceFk, [timestamp], latitude, longitude, altitude, speed, satellites, hdop, battery_voltage, is_stale, SYSUTCDATETIME()
    FROM @Locations;
END
