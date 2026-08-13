CREATE OR ALTER PROCEDURE usp_Device_GetByApiKeyHash
    @ApiKeyHash VARBINARY(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        id AS Id,
        device_id AS DeviceId,
        display_name AS DisplayName,
        api_key_hash AS ApiKeyHash,
        is_active AS IsActive,
        created_at AS CreatedAt
    FROM devices
    WHERE api_key_hash = @ApiKeyHash AND is_active = 1;
END
