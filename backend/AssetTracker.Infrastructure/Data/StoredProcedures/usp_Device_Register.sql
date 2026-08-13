CREATE OR ALTER PROCEDURE usp_Device_Register
    @DeviceId NVARCHAR(64),
    @DisplayName NVARCHAR(128) = NULL,
    @ApiKeyHash VARBINARY(64)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM devices WHERE device_id = @DeviceId)
    BEGIN
        THROW 50001, 'Device already exists.', 1;
    END

    INSERT INTO devices (device_id, display_name, api_key_hash, is_active, created_at)
    OUTPUT
        INSERTED.id AS Id,
        INSERTED.device_id AS DeviceId,
        INSERTED.display_name AS DisplayName,
        INSERTED.api_key_hash AS ApiKeyHash,
        INSERTED.is_active AS IsActive,
        INSERTED.created_at AS CreatedAt
    VALUES (@DeviceId, @DisplayName, @ApiKeyHash, 1, SYSUTCDATETIME());
END
