# Backend Data Model — SQL Server

## devices

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| `id` | `INT IDENTITY` | NO | auto | Internal PK |
| `device_id` | `VARCHAR(64)` | NO | `-` | Unique business key, e.g. `"goat-001"` |
| `display_name` | `VARCHAR(128)` | YES | `NULL` | |
| `api_key_hash` | `VARBINARY(64)` | NO | `-` | SHA-256 digest of the raw API key (32 bytes) |
| `is_active` | `BIT` | NO | `1` | |
| `created_at` | `DATETIME2` | NO | `SYSUTCDATETIME()` | |

Unique index on `device_id`.

## locations

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| `id` | `BIGINT IDENTITY` | NO | auto | Internal PK |
| `device_fk` | `INT` | NO | `-` | FK → `devices.id` |
| `timestamp` | `DATETIMEOFFSET` | NO | `-` | UTC, from payload |
| `latitude` | `FLOAT` | NO | `-` | WGS84 decimal degrees |
| `longitude` | `FLOAT` | NO | `-` | WGS84 decimal degrees |
| `altitude` | `FLOAT` | YES | `NULL` | Meters above sea level |
| `speed` | `FLOAT` | YES | `NULL` | Meters per second |
| `satellites` | `TINYINT` | YES | `NULL` | Count of satellites in view |
| `hdop` | `FLOAT` | YES | `NULL` | Horizontal dilution of precision |
| `battery_voltage` | `FLOAT` | YES | `NULL` | Volts |
| `is_stale` | `BIT` | NO | `0` | True when this is a fallback/last-known position |
| `created_at` | `DATETIME2` | NO | `SYSUTCDATETIME()` | Inserted by DB |

```sql
CREATE INDEX idx_locations_device_timestamp ON locations (device_fk, timestamp DESC);
```

## admin_users

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| `id` | `INT IDENTITY` | NO | auto | Internal PK |
| `username` | `VARCHAR(64)` | NO | `-` | Unique |
| `password_hash` | `VARCHAR(60)` | NO | `-` | BCrypt-encoded hash (self-contained: algorithm + cost + salt + hash) |
| `created_at` | `DATETIME2` | NO | `SYSUTCDATETIME()` | |

Unique index on `username`. No registration endpoint exists for this table — one dev admin user (`admin` / `ChangeMe123!`) is seeded via EF Core migration `HasData`. Rotate or remove this credential before any real deployment.

## Access pattern (hybrid)

| Table | Reads | Writes |
|---|---|---|
| `devices` | EF Core (`GetByDeviceIdAsync`) | Stored procedure (`usp_Device_Register`, `usp_Device_GetByApiKeyHash`) |
| `locations` | Stored procedure (`usp_Location_GetLatestByDevice`, `usp_Location_GetLatestForAllDevices`) | Stored procedure (`usp_Location_Insert`, `usp_Location_BatchInsert` via table-valued parameter) |
| `admin_users` | EF Core (`GetByUsernameAsync`) | Migration seed only, no runtime writes |

See `../diagrams.md` for the ORM/Dapper/stored-procedure layering.

## Stored Procedures

| Procedure | Purpose |
|---|---|
| `usp_Device_Register` | Insert a device row, return it |
| `usp_Device_GetByApiKeyHash` | Device auth lookup |
| `usp_Location_Insert` | Single location write |
| `usp_Location_BatchInsert` | Batch write via `LocationTableType` table-valued parameter |
| `usp_Location_GetLatestByDevice` | Single-device read — latest location for one device |
| `usp_Location_GetLatestForAllDevices` | Dashboard list read — latest location per device, across all devices with recorded history |
| `usp_Retention_PurgeOldLocations` | Deletes rows older than the retention window (default 30 days), returns count deleted |

## Retention
- 30-day rolling window, enforced by `usp_Retention_PurgeOldLocations`.
- Scheduling mechanism (SQL Server Agent job vs. hosted background service) is an open TBD — see the design doc, §11.
