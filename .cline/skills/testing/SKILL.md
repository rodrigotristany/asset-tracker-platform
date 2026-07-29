---
name: testing
description: Enforces testing standards across firmware, backend, and E2E with coverage targets and proper mocking
---

triggers:
  paths:
    - "**/test_*.py"
    - "**/*test*.py"
    - "**/*spec*.py"
    - "firmware/**/test_*.cpp"
  keywords:
    - "test"
    - "pytest"
    - "coverage"
    - "testcontainers"
    - "mock"
    - "e2e"

tool_restrictions:
  allowed:
    - read_files
    - write_files
    - run_commands
    - search_codebase
  disallowed:
    - fetch_web_content

## When to use

Use this skill whenever writing, modifying, or running tests for firmware, backend, or end-to-end pipelines. This skill should compose with domain skills (backend-development, firmware-development, frontend-development) to enforce quality gates while coding.

## Instructions

### Firmware
1. Primary validation is on-target on real ESP32 hardware with GPS connected.
2. Host-based unit tests are allowed for pure logic (NMEA parsing, `Location` construction) using mocks.
3. Test file naming: `test_<module_under_test>.cpp` (e.g., `test_gps_reader.cpp`). Co-locate tests with source files.
4. Prefer hand-rolled fakes for simple interfaces. Use Google Mock only if the interface complexity justifies it.
5. Mock adjacent modules when testing in isolation: provide mock/fake `GpsReader` when testing `ApiClient`, and vice versa.
6. Coverage target: 80% of non-boilerplate firmware code.
7. Use logic analyzer captures as test fixtures for UART behavior validation.

### Backend
1. Use pytest as the test runner.
2. Use PostgreSQL test containers (e.g., `pytest-docker` or `testcontainers`) for integration tests. Avoid SQLite in-memory for production-parity tests.
3. Test layout:
   - `backend/tests/unit/` — schemas, pure logic, utilities
   - `backend/tests/integration/` — API endpoints with test database
   - `backend/tests/e2e/` — full pipeline; manual execution in Phase 1
4. Every endpoint must have at least one integration test covering:
   - Happy path
   - Validation errors
   - Authentication/authorization failures
5. Pydantic schemas must have unit tests for validation rules, defaults, and serialization.
6. Services must have unit tests for business logic.
7. Use `pytest-mock` for patching dependencies. Use `responses` or `respx` for mocking external HTTP calls.
8. Dependency injection must be used to enable mock injection. Avoid monkey-patching.
9. Coverage target: 80% of `backend/app/` code.
10. Run `pytest` automatically after backend changes.

### E2E
1. Phase 1 E2E validation is a manual checklist executed on real hardware only.
   - Minimum: 2 physical devices streaming for 1 hour.
   - Validate: database entries match payloads, dashboard updates in real time, `is_stale` logic is observable.
2. Phase 2 E2E should be automated with hardware-in-the-loop or simulated device clients when feasible.
3. No CI-based E2E simulation for Phase 1 (real hardware is required).
