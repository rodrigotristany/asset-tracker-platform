# Asset Tracker Platform - Specification

## 1. Project Identity

**Name:** asset-tracker-platform  
**Goal:** Prove end-to-end GPS-to-dashboard pipeline with minimal viable features.  
**License:** MIT  
**Success Criteria:**
- Phase 1: Track 2 devices simultaneously on USB power, streaming GPS at 1Hz via WiFi to backend with live dashboard visualization.
- Phase 2: Scale to 22 devices with battery operation and evaluate BLE gateway architecture.

**Non-Goals (Out of Scope):**
- Analytics and AI
- Geofencing
- Mobile applications
- Complex dashboards beyond minimal latest-location view
- Performance optimization beyond proving the pipeline

---

## 2. Architecture Overview

For visual diagrams, see [System Diagrams](diagrams.md).

The system is composed of four completely independent layers. Each layer knows as little as possible about the others.

```
GPS Module
    │
    ▼
ESP32 Firmware (C++/ESP-IDF)
    │
    │ WiFi (Phase 1) / BLE Gateway (Phase 2)
    ▼
REST API (ASP.NET Core)
    │
    ▼
SQL Server Database
    │
    ▼
Web Dashboard (React + TypeScript)
```

**Design Principle:** Separation of concerns. Firmware classes, backend services, and frontend components are independently swappable.

---

## 3. Data Flow

### Phase 1: WiFi Streaming (Active Development)

1. GPS module acquires position fix (latitude, longitude, altitude, speed, satellites, HDOP)
2. ESP32 timestamps using GPS UTC time (primary) or NTP (fallback when WiFi connected)
3. Device creates `Location` message
4. POST to `POST /api/v1/locations` via HTTP over WiFi
5. Backend validates, persists to SQL Server
6. Dashboard queries `GET /api/v1/locations/{device_id}` and displays latest per device

### Phase 2: BLE Gateway Evaluation (Future)

- Devices record to local flash at high frequency
- BLE gateway (phone/hardware) collects stored sessions
- Gateway forwards batch to REST API
- Same backend and dashboard code; only firmware `NetworkManager` changes

---

## 4. Hardware Specification

### Phase 1 (Current)

| Component | Specification |
|-----------|---------------|
| **MCU** | ESP32-S3 with 1.47" LCD |
| **GPS** | GY-GPSV3 (U-blox NEO-6M/7M compatible) |
| **Connectivity** | USB-C power and debug |
| **Debug Tools** | USB Logic Analyzer (UART debugging) |
| **Power** | USB-powered (laptop) |
| **Prototyping** | Dupont cables |

### Phase 2 (Target - Battery)

| Aspect | Status |
|--------|--------|
| **Battery** | TBD (e.g., LiPo 18650 or 3.7V LiPo) |
| **Battery Management** | TBD (charging circuit required) |
| **Power Profile** | TBD (deep sleep vs. always-on evaluation) |
| **Device Count** | 22 simultaneous devices |

### Hardware Configuration File

All hardware-specific parameters are centralized in `firmware/config/device_config.hpp`:

- Device ID (string, e.g., `"goat-001"`)
- GPS UART port and pins
- Default WiFi SSID/PSK fallback (for provisioning)
- LCD enable/disable flag
- GPS update interval base
- Battery monitoring ADC pin

---

## 5. Firmware Specification

### 5.1 Technology Stack

- **Framework:** ESP-IDF v5.x
- **Language:** C++20/23 (as supported by ESP-IDF toolchain)
- **Build System:** CMake (ESP-IDF native)
- **RTOS:** FreeRTOS (provided by ESP-IDF)
- **GPS Parser:** TinyGPS++ (or custom NEMA parser if size constrained)

### 5.2 Directory Structure

```
firmware/
├── CMakeLists.txt
├── Makefile (optional wrapper)
├── main.cpp
├── sdkconfig (ESP-IDF configuration)
├── gps/
│   ├── gps_reader.hpp
│   └── gps_reader.cpp
├── network/
│   ├── wifi_manager.hpp
│   ├── wifi_manager.cpp
│   ├── api_client.hpp
│   ├── api_client.cpp
│   ├── ble_gateway.hpp          # Phase 2 stub
│   └── ble_gateway.cpp          # Phase 2 stub
├── display/
│   ├── lcd.hpp
│   └── lcd.cpp
├── config/
│   ├── device_config.hpp        # Hardware constants
│   └── settings.hpp             # Runtime settings (EEPROM/SPIFFS)
├── domain/
│   └── location.hpp
├── utils/
│   ├── timer.hpp
│   ├── logger.hpp
│   └── battery_monitor.hpp
└── storage/
    ├── local_queue.hpp           # SPIFFS/LittleFS circular buffer
    └── local_queue.cpp
```

### 5.3 Core Classes

| Class | Responsibility |
|-------|----------------|
| `GpsReader` | UART NMEA parsing, coordinate extraction, fix quality detection |
| `WifiManager` | Station mode, provisioning, connection management, NTP sync |
| `BleGatewayClient` | Phase 2: BLE peripheral role, session storage, gateway transfer |
| `ApiClient` | HTTP POST /locations, batch upload, retry logic, `is_stale` flag |
| `Display` | LCD output (debug phase; planned for removal in production) |
| `BatteryMonitor` | ADC voltage reading, percentage estimation |
| `Configuration` | Load/save settings to SPIFFS/LittleFS |
| `HttpClient` | Low-level HTTP transport (headers, body, TLS if needed) |
| `Timer` | FreeRTOS-based periodic task scheduling |
| `Logger` | Serial/logging abstraction |
| `LocalQueue` | Circular buffer on flash for store-and-forward |

### 5.4 GPS Behavior

- **Start Strategy:** Cold start on boot
- **Update Interval:**
  - Phase 1: Fixed 1 second (Hz streaming)
  - Phase 2: Adaptive based on speed (reduce frequency when stationary)
- **Sleep Mode:** GPS module kept active (no sleep commands) in Phase 1
- **Data Parsed:**
  - Latitude (double)
  - Longitude (double)
  - Altitude (meters)
  - Speed (km/h or m/s)
  - Satellites in view (uint8_t)
  - HDOP (horizontal dilution of precision)
  - Timestamp (UTC from GPS or NTP fallback)

### 5.5 WiFi Provisioning

**Phase 1 Approach:**
- Primary: Hardcoded fallback SSID/PSK in `device_config.hpp`
- Secondary: ESP-IDF Wi-Fi Provisioning Manager (SmartConfig / BLE provisioning) enabled for field updates
- Stored in SPIFFS after first successful connection

### 5.6 Timestamp Strategy

| Source | Priority | Notes |
|--------|----------|-------|
| GPS UTC | Primary | From NMEA GPRMC/GPGGA sentences; no drift |
| NTP | Fallback | Synced when WiFi connects; used only if GPS has no fix |
| RTC | Not required | GPS provides absolute time; no external RTC needed |

### 5.7 Error Recovery

| Failure Mode | Behavior |
|--------------|----------|
| **WiFi unavailable** | Buffer to `LocalQueue` (SPIFFS/LittleFS circular buffer) |
| **HTTP POST fails** | Retry immediately up to 3 times with exponential backoff (1s, 2s, 4s). After 3 failures: send **last known position** with `is_stale: true`. |
| **Queue full** | Overwrite oldest entries (circular buffer) |
| **GPS no fix** | Retry GPS read; if timeout (e.g., 30s), send last known position with `is_stale: true` and `satellites: 0` |

**Critical:** The `is_stale` field in the API payload MUST be set to `true` whenever the firmware sends any data that is not a fresh real-time fix. This includes:
- Queued positions flushed after WiFi reconnection
- Last-known-position fallbacks after GPS timeout
- Retry exhaustion positions

### 5.8 Power Management

**Phase 1:** No power optimization. Device stays fully awake, WiFi active, GPS polling at 1Hz. Powered via USB.

**Phase 2 (Evaluation):**
- Options: Deep sleep between fixes vs. always-on with reduced WiFi duty cycle
- Wake sources: Timer (RTC) or GPIO from GPS PPS (pulse-per-second)
- Target: Battery life measured in days to weeks depending on transmission frequency

### 5.9 OTA Updates

- **Phase 1:** Out of scope (nice-to-have only)
- **Phase 2:** Evaluate ESP-IDF OTA or custom HTTP update partition

### 5.10 Domain Model

```cpp
// domain/location.hpp
struct Location {
    std::string device_id;
    std::string timestamp;        // ISO 8601 UTC
    double latitude;
    double longitude;
    double altitude;
    double speed;
    uint8_t satellites;
    double hdop;
    double battery_voltage;
    bool is_stale;
};
```

---

## 6. Backend Specification

### 6.1 Technology Stack

| Component | Technology |
|-----------|------------|
| **Runtime** | .NET (latest LTS) |
| **Framework** | ASP.NET Core, Controller-based MVC |
| **Architecture** | Clean Architecture — `Domain` → `Application` → `Infrastructure` → `Api` |
| **Data Access** | Hybrid: EF Core (reads/simple CRUD) + Dapper-driven stored procedures (location/device writes, retention) |
| **Validation** | `System.ComponentModel.DataAnnotations` |
| **Database** | SQL Server |
| **Deployment** | Docker Compose (local and production) |
| **Host (Production)** | DigitalOcean Droplet (future) |
| **CI** | Azure Pipelines (build + test) |

This stack (vs. the originally-specced Python web-framework/ORM/database stack) was chosen deliberately to demonstrate .NET/C#, SQL Server (including stored procedures and database architecture), and Azure DevOps skills — see `docs/superpowers/specs/2026-08-12-backend-csharp-design.md` for the full rationale.

### 6.2 Hosting Strategy

**Local Development:**
- Docker Compose orchestrates the API + SQL Server (`mcr.microsoft.com/mssql/server:2022-latest`)
- Dashboard served by a separate Vite dev server during development

**Production (DigitalOcean Droplet):**
- Docker Compose preferred for reproducibility and one-command deploys
- **Known risk:** SQL Server needs materially more RAM (~2GB minimum) than the database engine it replaces; the droplet's memory sizing needs revisiting before a real production deploy (open TBD)

### 6.3 Authentication

| Actor | Mechanism | Notes |
|-------|-----------|-------|
| **Devices** | API key in `X-API-Key` header | Base64-encoded 32 random bytes; validated by re-hashing (SHA-256) and comparing to `devices.api_key_hash` |
| **Dashboard Admin** | JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`) | Obtained via `POST /api/v1/auth/login`; one dev-seeded admin user, no self-registration |

### 6.4 API Endpoints

| Method | Endpoint | Purpose | Auth |
|--------|----------|---------|------|
| `POST` | `/api/v1/auth/login` | Admin login, issues JWT | None |
| `POST` | `/api/v1/devices` | Register a device, returns its API key once | JWT |
| `POST` | `/api/v1/locations` | Single location upload | Device API key |
| `POST` | `/api/v1/locations/batch` | Batch upload (reconnection scenarios) | Device API key |
| `GET` | `/api/v1/locations/{deviceId}` | Latest location for dashboard | JWT |
| `GET` | `/api/v1/health` | Health check / connectivity verification | None |

`/api/v1/devices` and `/api/v1/auth/login` are additions beyond the original Python-era spec — they exist because the expanded, normalized schema (§6.5) requires a real `devices` row (with a hashed API key) before any location can be written.

### 6.5 Data Schema

```sql
CREATE TABLE devices (
    id INT IDENTITY PRIMARY KEY,
    device_id VARCHAR(64) NOT NULL UNIQUE,
    display_name VARCHAR(128) NULL,
    api_key_hash VARBINARY(64) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE locations (
    id BIGINT IDENTITY PRIMARY KEY,
    device_fk INT NOT NULL FOREIGN KEY REFERENCES devices(id),
    [timestamp] DATETIMEOFFSET NOT NULL,
    latitude FLOAT NOT NULL,
    longitude FLOAT NOT NULL,
    altitude FLOAT NULL,
    speed FLOAT NULL,
    satellites TINYINT NULL,
    hdop FLOAT NULL,
    battery_voltage FLOAT NULL,
    is_stale BIT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE INDEX idx_locations_device_timestamp ON locations (device_fk, [timestamp] DESC);

CREATE TABLE admin_users (
    id INT IDENTITY PRIMARY KEY,
    username VARCHAR(64) NOT NULL UNIQUE,
    password_hash VARCHAR(60) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
```

**Retention:** 30-day rolling window, enforced by the `usp_Retention_PurgeOldLocations` stored procedure. Scheduling mechanism (SQL Agent job vs. hosted background service) is an open TBD.

See `specs/backend/models.md` for the full table/procedure reference and the EF Core/Dapper access-pattern split.

### 6.6 Request/Response Contracts

**POST /api/v1/locations**
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

**Response (201 Created)**
```json
{
    "id": 1234,
    "status": "accepted"
}
```

**Errors:**
- `400 Bad Request` — Validation error (missing fields, invalid coordinates)
- `401 Unauthorized` — Missing or invalid API key / JWT
- `404 Not Found` — Unknown `deviceId` on a location write
- `409 Conflict` — Duplicate `deviceId` on device registration

See `specs/backend/api.md` for the full endpoint reference including the new `/devices` and `/auth/login` contracts.

### 6.7 Additional Backend Features

- **CORS:** Enabled for local dashboard development (`http://localhost:5173`)
- **OpenAPI:** Auto-generated Swagger UI at `/swagger` (Swashbuckle.AspNetCore) in the Development environment
- **Compression:** Gzip enabled for responses (`Microsoft.AspNetCore.ResponseCompression`)
- **Logging:** Structured logs; unhandled exceptions logged with request trace ID via `ErrorHandlingMiddleware`

---

## 7. Web Dashboard Specification

### 7.1 Technology Stack

| Component | Technology |
|-----------|------------|
| **Framework** | React 18+ |
| **Language** | TypeScript |
| **Build Tool** | Vite |
| **Styling** | Tailwind CSS |
| **State Management** | TanStack Query (React Query) for server state |
| **Routing** | React Router v6 |
| **Maps** | None (not required) |

### 7.2 Hosting Strategy

**Phase 1:**
- Vite dev server during development
- ASP.NET Core serves built static files (`/static`) in production (via `UseStaticFiles`)

**Phase 2:**
- Built with `vite build`
- `dist/` copied to droplet
- Served by ASP.NET Core or nginx on droplet

### 7.3 Features (Phase 1)

- **Device List View:** Table showing all registered devices with latest location, timestamp, and status
- **Device Detail View:** Latest GPS data (lat, lon, speed, altitude, satellites, battery)
- **Status Indicators:**
  - Online/Offline (based on recent data timestamp)
  - Battery level (visual indicator)
  - Stale data warning (when `is_stale: true`)
- **Read-Only:** No edit capabilities; admin view only
- **Authentication:** JWT-based session login

### 7.4 Out of Scope

- Historical path replay
- Maps / map markers
- Real-time WebSocket updates (polling is acceptable)
- Multi-user roles (admin only)

---

## 8. API Data Contracts

### 8.1 Location Schema (Shared)

This schema is used by both firmware and backend to ensure consistency.

```typescript
// Shared TypeScript type (also reflected in AssetTracker.Application.Dtos.LocationCreateDto)
interface Location {
    deviceId: string;
    timestamp: string;          // ISO 8601 UTC
    latitude: number;
    longitude: number;
    altitude?: number;
    speed?: number;
    satellites?: number;
    hdop?: number;
    batteryVoltage?: number;
    isStale: boolean;
}
```

### 8.2 Retry Semantics

- **Error:** Server returns 5xx or network timeout
- **Immediate Retry:** Up to 3 attempts with exponential backoff (1s, 2s, 4s)
- **Fallback:** After 3 immediate failures, send `lastKnownPosition` with `is_stale: true`
- **Store-and-Forward:** If WiFi is down, buffer in `LocalQueue` (SPIFFS circular buffer). On reconnection, flush queue. If queue is full, overwrite oldest entries.

---

## 9. Technology Stack Summary

### Firmware (ESP32)

- **Framework:** ESP-IDF v5.x
- **Language:** C++20/23 (as supported by toolchain)
- **Build:** CMake + ESP-IDF build system
- **RTOS:** FreeRTOS (built-in)
- **GPS Library:** TinyGPS++
- **Storage:** SPIFFS/LittleFS for settings and local queue

### Backend

- **Language:** C# / .NET (latest LTS)
- **Framework:** ASP.NET Core (Controller MVC)
- **Architecture:** Clean Architecture (Domain/Application/Infrastructure/Api)
- **Data Access:** EF Core (reads/simple CRUD) + Dapper-driven stored procedures (writes)
- **Database:** SQL Server
- **Deployment:** Docker Compose
- **CI:** Azure Pipelines

### Frontend

- **Framework:** React 18+
- **Language:** TypeScript
- **Build:** Vite
- **Styling:** Tailwind CSS
- **State:** TanStack Query + Zustand

### Development Environment

- **Host OS:** Linux Fedora Workstation 43
- **Toolchain:** ESP-IDF v5.x with C++ compiler
- **Firmware IDE:** PlatformIO (optional) or ESP-IDF CLI
- **Docker:** Required for backend consistency
- **Git:** Version control (already initialized)

---

## 10. Milestones (Ordered, Testable)

### Phase 1 Milestones

1. **"Hello World" on LCD** — Display initialization and static text
2. **Raw GPS NMEA Reading** — UART receive and log NMEA sentences; verify with logic analyzer
3. **Parse Coordinates** — Extract lat, lon, alt, speed, sats, HDOP from NMEA
4. **Display Parsed Data** — Show coordinates on LCD
5. **WiFi Connection** — Connect to AP using configured credentials
6. **HTTP POST to API** — Send parsed location to local ASP.NET Core server
7. **SQL Server Persistence** — Backend receives and stores location in database
8. **Live Dashboard** — Web page displays latest location per device

**Phase 1 Exit Criteria:** 2 devices stream 1Hz data continuously for 24 hours with live dashboard updates.

### Phase 2 Milestones (Future)

1. **Battery Hardware Integration** — Integrate LiPo + charging circuit
2. **Power Profiling** — Measure current draw in active vs. sleep modes
3. **BLE Gateway Prototype** — Implement `BleGatewayClient` stub; test with phone/hub
4. **Local Flash Storage** — Implement `LocalQueue` with high-frequency buffering
5. **Batch Upload Protocol** — Implement `/api/v1/locations/batch` endpoint and firmware batch sender
6. **Scale Test** — 22 devices streaming simultaneously (WiFi or BLE gateway)
7. **Production Deploy** — Docker Compose on DigitalOcean droplet

---

## 11. Testing Strategy

### Firmware Tests

- **On-target only:** Tests run on real ESP32 hardware with GPS module connected
- **Host-based unit tests:** Optional using mocks for pure logic (e.g., `Location` parsing)
- **Test Fixtures:** Logic analyzer captures for UART validation

### Backend Tests

- **Unit Tests:** `dotnet test` (xUnit) for business logic (e.g., retry logic, `is_stale` flag handling)
- **Integration Tests:** xUnit + `WebApplicationFactory` against the ASP.NET Core app with a Testcontainers-provisioned SQL Server
- **Coverage Target:** Core API paths and error handling

### End-to-End Tests

- Full pipeline: GPS → Firmware → API → Database → Dashboard
- Minimum: 2 physical devices streaming simultaneously for 1 hour
- Validation: Database entries match expected payloads; dashboard updates in real-time

---

## 12. Documentation Plan

| Document | Path | Content |
|----------|------|---------|
| **Project README** | `README.md` | Project overview, quick start, wiring diagram |
| **Hardware Wiring** | `docs/hardware-wiring.md` | Pinout, logic analyzer probe points, USB connections |
| **API Reference** | `docs/api.md` | OpenAPI auto-generated (Swagger UI) + manual endpoint descriptions |
| **Architecture** | `docs/architecture.md` | System diagrams, data flow, class relationships |
| **Firmware Setup** | `docs/firmware-setup.md` | ESP-IDF installation, build, flash, monitor |
| **Backend Setup** | `docs/backend-setup.md` | Docker Compose, EF Core migrations, .NET SDK setup |
| **Dashboard Setup** | `docs/dashboard-setup.md` | Node.js, Vite dev server, build for production |

---

## 13. CI/CD

**Backend:** Azure Pipelines (`azure-pipelines.yml`, repo root) runs restore/build/test on every push to `main`, including integration tests against a Testcontainers-provisioned SQL Server. No deploy stage yet.

**Phase 2 (Planned):**
- Extend the Azure Pipelines definition with lint/type-check for firmware and frontend
- Firmware build verification (compile check without flash)
- Docker image build and push on merge to main
- **Artifacts:**
  - Firmware binaries (auto-generated)
  - Backend Docker images
  - Dashboard static build

---

## 14. Repository Structure

```
asset-tracker-platform/
├── firmware/              # ESP-IDF C++ project
│   ├── CMakeLists.txt
│   ├── main.cpp
│   ├── gps/
│   ├── network/
│   ├── display/
│   ├── config/
│   ├── domain/
│   ├── utils/
│   └── storage/
├── backend/               # ASP.NET Core Clean Architecture solution
│   ├── AssetTracker.sln
│   ├── AssetTracker.Domain/
│   ├── AssetTracker.Application/
│   ├── AssetTracker.Infrastructure/
│   ├── AssetTracker.Api/
│   │   └── Dockerfile
│   ├── AssetTracker.Tests/
│   └── docker-compose.yml
├── dashboard/             # React + TypeScript frontend
│   ├── src/
│   ├── package.json
│   ├── vite.config.ts
│   ├── tailwind.config.js
│   └── tsconfig.json
├── docs/                  # Documentation
│   ├── hardware-wiring.md
│   ├── api.md
│   ├── architecture.md
│   ├── firmware-setup.md
│   ├── backend-setup.md
│   └── dashboard-setup.md
├── specs/                 # Specifications
│   └── spec.md
├── .clinerules/           # AI coding agent guidelines
├── .gitignore
├── LICENSE
└── README.md
```

---

## 15. Constraints & Assumptions

- **Development Host:** Linux Fedora Workstation 43
- **Docker:** Required for backend development and eventual production
- **Production Host:** Local machine initially; DigitalOcean Droplet in future (existing droplet available)
- **No Map Provider:** Dashboard does not integrate Leaflet, Google Maps, or Mapbox
- **Dashboard Access:** Read-only, authenticated, single admin role for Phase 1
- **Data Retention:** 30-day rolling window; automated cleanup
- **LCD Display:** Debug tool only; architecturally separated for easy removal

---

## 16. Open TBDs

| Topic | Status |
|-------|--------|
| **Adaptive GPS interval algorithm** | TBD (Phase 2; thresholds for speed-based frequency change) |
| **Battery hardware specifics** | TBD (LiPo type, capacity, charging circuit) |
| **Production Droplet specs** | TBD (Size, region, backup strategy) |
| **BLE Gateway hardware** | TBD (Phone app vs. dedicated gateway hardware) |
| **Dashboard auth persistence** | TBD (Session cookie vs. localStorage JWT) |
| **Firmware OTA strategy** | TBD (ESP-IDF OTA partitions vs. custom HTTP) |

---

## 17. Phase 1 vs. Phase 2 Summary

| Aspect | Phase 1 (Current) | Phase 2 (Future) |
|--------|-------------------|------------------|
| **Device Count** | 2 (debug) | 22 (target) |
| **Power Source** | USB (laptop) | Battery (LiPo) |
| **Network** | WiFi @ 1Hz | BLE Gateway + Batch upload |
| **GPS Interval** | Fixed 1 second | Adaptive based on speed |
| **Display** | LCD (debug) | None (removed) |
| **Local Storage** | Minimal buffering | Flash queue for high-frequency sessions |
| **Backend Deployment** | Local Docker | Docker Compose on DigitalOcean Droplet |
| **Dashboard** | Localhost Vite dev | Static files served from droplet |
| **Authentication** | API key + JWT | Same + future multi-user roles |

---

*Specification Version: 1.0*  
*Date: 2026-07-29*  
*Author: Rodrigo Tristany*
