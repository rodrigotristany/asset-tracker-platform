CREATE OR ALTER PROCEDURE usp_Retention_PurgeOldLocations
    @RetentionDays INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Cutoff DATETIMEOFFSET = DATEADD(DAY, -@RetentionDays, SYSUTCDATETIME());
    DECLARE @DeletedCount INT;

    DELETE FROM locations WHERE [timestamp] < @Cutoff;
    SET @DeletedCount = @@ROWCOUNT;

    SELECT @DeletedCount AS DeletedCount;
END
