# Frontend/Backend Spec Alignment — Design

**Date:** 2026-08-14
**Author:** Rodrigo Tristany
**Status:** Approved

## 1. Context

`docs/superpowers/specs/2026-08-12-backend-csharp-design.md` (Approved) rewrote the backend as C#/.NET, changing the database schema (device FK constraint, `usp_Device_Register`, `usp_Location_GetLatestByDevice`) and API surface (new `POST /api/v1/devices`, new `POST /api/v1/auth/login`). The frontend specs in `specs/frontend/` (`pages.md`, `services.md`, `types.md`) were written against the original Python spec and predate this rewrite. Comparing the two surfaced three gaps that need resolving before implementation:

1. `getDevicesSummary()` in `services.md` is marked TBD, but the backend design's endpoint table had no route to back it.
2. The backend design's auth wording ("issues a JWT ... cookie-based session") contradicted the frontend's bearer-token assumption (`AuthState.token`, `Authorization` header).
3. The backend design made device registration a hard prerequisite (FK constraint on `locations.device_fk`), but the frontend spec declared Phase 1 to have no create flows at all — leaving no way to register a device.

This document is a spec-alignment pass, not a new feature design: it amends the backend design doc and updates the three frontend spec files so both sides agree on the same contract. No backend or frontend code exists yet, so this is doc-only.

## 2. Backend Design Amendments

Two changes to `2026-08-12-backend-csharp-design.md`:

- **§5 Authentication** — reworded to remove the contradictory "cookie-based session" language. Final flow: `POST /api/v1/auth/login` returns the JWT in the JSON response body as `{ token: string }`. The frontend stores it and sends `Authorization: Bearer <token>` on subsequent requests. No cookie is used.
- **§6 API Endpoints** — add a new row:

  | Method | Route | Auth | Notes |
  |---|---|---|---|
  | `GET` | `/api/v1/devices` | JWT (admin) | **New** — latest location per device, backed by `usp_Location_GetLatestByDevice` (defined in §3 but previously had no endpoint calling it). Powers `DevicesPage`. |

## 3. Frontend Spec Changes

### `specs/frontend/pages.md`

- New route: `/devices/new` → `AddDevicePage`, reached via a "+ Add Device" button on `DevicesPage`.
- `AddDevicePage`:
  - Form fields: Device ID (required, matches `devices.device_id`), Display Name (optional, matches `devices.display_name`).
  - Submits to `POST /api/v1/devices`.
  - On success, shows the returned API key **once**, with a clear warning that it will not be shown again, then links back to `/devices`.
- `DevicesPage` itself is unchanged (still a read-only table); only gains the entry point button.
- Notes section: "Phase 1: no edit/create/delete flows" → "Phase 1: device registration only (no edit/delete); locations remain read-only, no create/edit UI for location data."

### `specs/frontend/services.md`

- `getDevicesSummary()`: implement as `GET /api/v1/devices` → `DeviceSummary[]`. No longer TBD.
- `login(username, password)`: `POST /api/v1/auth/login` with `{ username, password }` body, response `{ token: string }`. Wraps into `AuthState`, calls `this.setToken(token)` internally. No longer TBD.
- Add `registerDevice(deviceId: string, displayName?: string): Promise<DeviceRegistrationResult>` → `POST /api/v1/devices`.
- Fix existing spec typo: `getLatestLocation` calls `this.authHeader()` but the method is defined as `authHeaders()`. Unify to `authHeaders()`.

### `specs/frontend/types.md`

- Add:
  ```typescript
  export interface DeviceRegistrationRequest {
    deviceId: string;
    displayName?: string;
  }

  export interface DeviceRegistrationResult {
    deviceId: string;
    apiKey: string; // shown once, never retrievable again
  }
  ```
- `Location`, `DeviceSummary`, and `AuthState` are unchanged — `DeviceSummary.status` remains client-derived from `isStale`/timestamp age, consistent with the existing logic already documented in `pages.md`.

## 4. Non-Goals

- No device edit/delete UI (registration only).
- No changes to `Location`/`DeviceSummary`/`AuthState` shapes.
- No changes to firmware or the location-ingest endpoints (`POST /api/v1/locations`, `/batch`).

## 5. Open TBDs

None — all three ambiguities identified in §1 were resolved in this pass.
