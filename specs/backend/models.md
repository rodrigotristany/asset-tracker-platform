# Backend Models — SQLAlchemy

## Location

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| `id` | `Integer` | NO | `serial PRIMARY KEY` | Internal PK |
| `device_id` | `String(64)` | NO | `-` | Indexed; supplied by device |
| `timestamp` | `DateTime(timezone=True)` | NO | `-` | UTC, from payload |
| `latitude` | `Float` | NO | `-` | WGS84 decimal degrees |
| `longitude` | `Float` | NO | `-` | WGS84 decimal degrees |
| `altitude` | `Float` | YES | `NULL` | Meters above sea level |
| `speed` | `Float` | YES | `NULL` | Meters per second |
| `satellites` | `SmallInteger` | YES | `NULL` | Count of satellites in view |
| `hdop` | `Float` | YES | `NULL` | Horizontal dilution of precision |
| `battery_voltage` | `Float` | YES | `NULL` | Volts |
| `is_stale` | `Boolean` | NO | `FALSE` | True when this is a fallback/last-known position |
| `created_at` | `DateTime(timezone=True)` | NO | `now()` | Inserted by DB |

## Indexes
```sql
CREATE INDEX idx_locations_device_timestamp ON locations (device_id, timestamp DESC);
```

## Retention
- 30-day rolling window.
- Enforced by scheduled job/cron (out of scope for Phase 1 schema).
