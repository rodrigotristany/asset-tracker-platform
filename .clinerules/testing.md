---
paths:
  - "firmware/**"
  - "backend/**"
---

# Testing Guidelines

## Firmware Testing
- **Primary:** On-target tests on real ESP32 hardware with GPS module connected.
- **Host-based unit tests:** Allowed for pure logic (e.g., NMEA parsing, Location construction) using mocks.
- **Naming:** `test_<module_under_test>.cpp` (e.g., `test_gps_reader.cpp`).
- **Mocking:** Provide mock/fake implementations for `GpsReader` when testing `ApiClient` in isolation, and vice versa.
- **Coverage Target:** 80% of non-boilerplate firmware code.
- **Test Location:** Co-located with source files (e.g., `firmware/gps/test_gps_reader.cpp`).

## Backend Testing
- **Database:** Use a **PostgreSQL test container** (e.g., `pytest-docker` or `testcontainers`) for integration tests. Avoid SQLite in-memory for production-parity tests.
- **Coverage Target:** 80% of `backend/app/` code.
- **Test Organization:**
  - `backend/tests/unit/` — Pure logic, schemas, utilities.
  - `backend/tests/integration/` — API endpoints with test database.
  - `backend/tests/e2e/` — Full pipeline against running services (manual in Phase 1).

### Backend Test Requirements
- **All endpoints** must have at least one integration test covering:
  - Happy path
  - Validation errors
  - Authentication/authorization failures
- **Pydantic schemas** must have unit tests for validation rules, defaults, and serialization.
- **Business logic** in services must be unit-tested.

## Mocking Strategy
### Firmware
- Prefer **hand-rolled fakes** for simple interfaces; use Google Mock only if interface complexity justifies it.
- All mocks must implement the same interface as the real component.

### Backend
- Use `pytest-mock` for patching dependencies.
- Use `responses` or `respx` for mocking external HTTP calls.
- Dependency injection must be used to enable mock injection; avoid monkey-patching in tests.

## E2E Validation
- **Phase 1:** E2E tests are **manual checklists** executed on real hardware.
  - Minimum: 2 physical devices streaming for 1 hour, verifying database entries and dashboard updates.
- **Phase 2:** Automate E2E with hardware-in-the-loop (if possible) or simulated device clients.
- No CI-based E2E simulation for Phase 1 (real hardware required).
