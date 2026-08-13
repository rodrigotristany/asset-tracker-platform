---
paths:
  - "firmware/**"
  - "backend/**"
---

# Architecture Rules

## Firmware Class Boundaries
- `GpsReader` must **emit events** or return data to the orchestrator; it must not call `WifiManager` or `ApiClient` directly.
- `Display` reads state from the orchestrator (`main.cpp`), not from other modules directly.
- `main.cpp` is the **composer root**: it instantiates all major classes and wires dependencies. No other class should instantiate `WifiManager`, `GpsReader`, etc.
- Communication between components:
  - Synchronous: Direct return values.
  - Asynchronous: Event queues or callbacks (e.g., "new location available").

## Backend Layer Separation
- Clean Architecture, four projects, dependencies point inward only:
  1. **Api** (`AssetTracker.Api`): Controllers handle HTTP, call `Application` services.
  2. **Application** (`AssetTracker.Application`): Services (business logic), DTOs, repository interfaces.
  3. **Infrastructure** (`AssetTracker.Infrastructure`): EF Core `DbContext`, repository implementations (Dapper/stored-procedure or EF Core), security (password hashing, JWT generation).
  4. **Domain** (`AssetTracker.Domain`): Entities only, zero project references.
- `Application.Dtos` must **never** reference `Domain` entities directly in their public shape, and must never import EF Core or Dapper types.
- Controllers must go through the service layer; no direct repository or `DbContext` access from a controller.

## Cross-Layer Independence
- Firmware `domain::Location` must not depend on `network`, `storage`, `display`, or `utils` classes.
- Backend `LocationCreateDto`/`LocationReadDto` mirror the firmware struct but are **independent types**; no shared code between firmware and backend.
- Dashboard API client types must match backend DTOs exactly (or be generated from OpenAPI spec).

## Import / Dependency Allowlists
### Firmware
- `gps/gps_reader.hpp` may depend on ESP-IDF `driver/uart.h` and `driver/gpio.h`.
- `gps/gps_reader.hpp` must **not** depend on `network/wifi_manager.hpp` or `network/api_client.hpp`.
- `network/api_client.hpp` may depend on `network/wifi_manager.hpp` (needs WiFi before sending).
- `domain/location.hpp` must have **zero** dependencies on other firmware modules.

### Backend
- `AssetTracker.Api` may reference `AssetTracker.Application` and `AssetTracker.Infrastructure` (for DI registration only — controllers themselves inject `Application` interfaces, never `Infrastructure` concrete types).
- `AssetTracker.Application` may reference `AssetTracker.Domain` only.
- `AssetTracker.Infrastructure` may reference `AssetTracker.Application` and `AssetTracker.Domain`.
- `AssetTracker.Domain` has zero project references.
- **No circular project references** — enforced structurally by the four-project split; the .NET build itself rejects a cycle.

## Configuration Isolation
- `firmware/config/device_config.hpp` contains **read-only constants** compiled into the firmware (Device ID, pins, default SSID).
- Runtime settings (WiFi credentials, API endpoint) are stored in `firmware/config/settings.hpp` and persisted to SPIFFS/LittleFS.
- Backend configuration is loaded from `appsettings.json` (fake local-dev defaults only) and environment variables (`ConnectionStrings__Default`, `Jwt__Key`, etc. — real values in any non-local environment).
- No hardcoded secrets or environment-specific values in source code.
