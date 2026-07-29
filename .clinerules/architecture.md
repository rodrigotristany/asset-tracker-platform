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
- Strict 3-layer architecture:
  1. **Routes** (`app/routers/`): Handle HTTP, call services.
  2. **Services** (`app/services/`): Business logic, orchestration.
  3. **Repositories** (`app/repositories/`): Database access only.
- Pydantic DTOs must **never** import SQLAlchemy models.
- Routers must go through the service layer; no direct repository access from routes.

## Cross-Layer Independence
- Firmware `domain::Location` must not depend on `network`, `storage`, `display`, or `utils` classes.
- Backend Pydantic schemas for `Location` mirror the firmware struct but are **independent types**; no shared code between firmware and backend.
- Dashboard API client types must match backend Pydantic models exactly (or be generated from OpenAPI spec).

## Import / Dependency Allowlists
### Firmware
- `gps/gps_reader.hpp` may depend on ESP-IDF `driver/uart.h` and `driver/gpio.h`.
- `gps/gps_reader.hpp` must **not** depend on `network/wifi_manager.hpp` or `network/api_client.hpp`.
- `network/api_client.hpp` may depend on `network/wifi_manager.hpp` (needs WiFi before sending).
- `domain/location.hpp` must have **zero** dependencies on other firmware modules.

### Backend
- `app/routers/locations.py` may import from `app/services/location_service.py`.
- `app/routers/locations.py` must **not** import from `app/repositories/`.
- `app/schemas/` must not import from `app/models/`.
- **No circular dependencies** between any modules.

## Configuration Isolation
- `firmware/config/device_config.hpp` contains **read-only constants** compiled into the firmware (Device ID, pins, default SSID).
- Runtime settings (WiFi credentials, API endpoint) are stored in `firmware/config/settings.hpp` and persisted to SPIFFS/LittleFS.
- Backend configuration is loaded exclusively from `.env` files via `pydantic-settings`.
- No hardcoded secrets or environment-specific values in source code.
