# Backend API

## Endpoints

| Method | Path | Auth | Body | Response | Purpose |
|--------|------|------|------|----------|---------|
| `POST` | `/api/v1/locations` | `X-API-Key` | `LocationCreate` | `LocationResponse` | Single upload |
| `POST` | `/api/v1/locations/batch` | `X-API-Key` | `BatchLocationCreate` | `LocationResponse` | Batch upload |
| `GET` | `/api/v1/locations/{device_id}` | JWT Session | `-` | `list[LocationRead]` | Latest for dashboard |
| `GET` | `/api/v1/health` | None | `-` | `{"status": "ok"}` | Health/connectivity |

## Request/Response Contracts

See `schemas.md` for exact DTOs.

### POST /api/v1/locations

**Request:**
```json
{
  "deviceId": "goat-001",
  "timestamp": "2026-07-29T13:20:00Z",
  "latitude": -31.4231,
  "longitude": -62.0834,
  "altitude": 142.1,
  "speed": 0.4,
  "satellites": 9,
  "hdop": 0.8,
  "batteryVoltage": 3.7,
  "isStale": false
}
```

**Success:** `201 Created`
```json
{"id": 1234, "status": "accepted"}
```

**Error:** `400/401` with standard envelope.

## Notes
- Dashboard auth uses JWT with cookie-based session by default.
- OpenAPI served at `/docs` and `/redoc`.
- Gzip enabled at ASGI/reverse proxy level.
