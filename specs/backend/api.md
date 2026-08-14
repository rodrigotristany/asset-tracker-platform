# Backend API

## Endpoints

| Method | Path | Auth | Body | Response | Purpose |
|--------|------|------|------|----------|---------|
| `POST` | `/api/v1/auth/login` | None | `LoginRequestDto` | `LoginResponseDto` | Admin login, issues JWT |
| `POST` | `/api/v1/devices` | JWT | `DeviceRegisterRequestDto` | `DeviceRegisterResponseDto` | Register a device, returns its API key once |
| `POST` | `/api/v1/locations` | `X-API-Key` | `LocationCreateDto` | `LocationCreateResponseDto` | Single upload |
| `POST` | `/api/v1/locations/batch` | `X-API-Key` | `LocationBatchCreateDto` | `LocationCreateResponseDto[]` | Batch upload |
| `GET` | `/api/v1/locations/{deviceId}` | JWT | `-` | `LocationReadDto` | Latest location for one device; `404` if none recorded yet |
| `GET` | `/api/v1/devices` | JWT | `-` | `LocationReadDto[]` | Latest location per device, one row per registered device (dashboard list) |
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

**Error:** `400/401` with standard envelope. Also `403` (`"error": "FORBIDDEN"`) if the authenticated device's own identity doesn't match the request's `deviceId` — see Notes below.

### POST /api/v1/devices

**Request:**
```json
{"deviceId": "goat-001", "displayName": "Goat 001"}
```

**Success:** `201 Created`
```json
{"deviceId": "goat-001", "apiKey": "base64-encoded-32-random-bytes"}
```

The `apiKey` is shown exactly once — only its SHA-256 hash is stored (`devices.api_key_hash`). There is no retrieval endpoint; losing it means re-registering under a new device ID.

**Error:** `401` (missing/invalid JWT), `409` (device ID already registered).

### POST /api/v1/auth/login

**Request:**
```json
{"username": "admin", "password": "..."}
```

**Success:** `200 OK`
```json
{"token": "<jwt>"}
```

**Error:** `401` on wrong username/password (standard envelope, `"error": "INVALID_CREDENTIALS"`).

### GET /api/v1/locations/{deviceId}

**Success:** `200 OK`
```json
{
    "id": 1234,
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

**Error:** `404` (`"error": "LOCATION_NOT_FOUND"`) if the device has never reported a location. `401` (missing/invalid JWT).

### GET /api/v1/devices

**Success:** `200 OK`
```json
[
    {
        "id": 1234,
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
]
```

One `LocationReadDto` per device that has recorded at least one location (its most recent location) — a device with no location history is simply absent from the list, not present with null fields. Backed by `usp_Location_GetLatestForAllDevices` (a distinct stored procedure from `usp_Location_GetLatestByDevice` above: the two take different parameters — one device vs. all — and once a migration ships it can never be edited in place, so the all-devices query could not simply extend the existing procedure). The dashboard derives online/offline/stale status client-side from `isStale` and timestamp age — see `specs/frontend/pages.md`.

**Error:** `401` (missing/invalid JWT).

## Notes
- Devices authenticate via `X-API-Key` header (raw key, base64-encoded 32 random bytes — validated by decoding and re-hashing with SHA-256, then comparing to `devices.api_key_hash`).
- **Device ownership check:** `POST /api/v1/locations` and `POST /api/v1/locations/batch` additionally verify that the authenticated device's own identity matches the `deviceId` in the request body (for batch, every item's `deviceId` too) — an authenticated device cannot write location data on behalf of a different `deviceId`. A mismatch raises `DeviceOwnershipMismatchException`, mapped by `ErrorHandlingMiddleware` to `403 Forbidden` with `"error": "FORBIDDEN"`.
- Dashboard admin authenticates via JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`), obtained from `/api/v1/auth/login`.
- Swagger UI served at `/swagger` in the Development environment (via Swashbuckle.AspNetCore) — the ASP.NET Core equivalent of the old Python-era spec's auto-generated docs route.
- Gzip response compression enabled via `Microsoft.AspNetCore.ResponseCompression`.
