CREATE TYPE LocationTableType AS TABLE
(
    [timestamp]      DATETIMEOFFSET NOT NULL,
    latitude         FLOAT NOT NULL,
    longitude        FLOAT NOT NULL,
    altitude         FLOAT NULL,
    speed            FLOAT NULL,
    satellites       TINYINT NULL,
    hdop             FLOAT NULL,
    battery_voltage  FLOAT NULL,
    is_stale         BIT NOT NULL
);
