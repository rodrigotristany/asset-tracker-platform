# Frontend/Backend Spec Alignment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring `specs/frontend/*.md` and `docs/superpowers/specs/2026-08-12-backend-csharp-design.md` into agreement, per the decisions recorded in `docs/superpowers/specs/2026-08-14-frontend-backend-alignment-design.md`.

**Architecture:** This is a documentation-only change set — no application code exists yet. Each task edits one spec file (exact `old_string`/`new_string` blocks are given), verifies the edit with `grep`, and commits.

**Tech Stack:** Markdown spec files only. No build/test tooling involved.

## Global Constraints

- Doc-only change — do not create or modify any source code files (no `.cs`, `.ts`, `.tsx`, etc.).
- JSON/API field names stay camelCase — existing convention in both specs, unchanged by this plan.
- Preserve existing markdown table formatting (pipe-aligned columns) in any table edited.
- Every task's edits must be applied with the Edit tool using the exact `old_string`/`new_string` blocks given — do not paraphrase.

**Scope note (added after Tasks 1-4 were reviewed):** the final whole-branch review found that Tasks 1-4 only updated the *decision-record* backend design doc (`docs/superpowers/specs/2026-08-12-backend-csharp-design.md`), not the *implementation-facing* specs (`specs/backend/api.md`, `specs/spec.md`) — so the branch's stated goal (frontend and backend specs agree) was still incomplete. Tasks 5-7 close that gap and were approved by the human partner to expand this plan's scope beyond the original four files.

---

### Task 1: Amend backend design doc (auth wording + new endpoint row)

**Files:**
- Modify: `docs/superpowers/specs/2026-08-12-backend-csharp-design.md:96-116`

**Interfaces:**
- Consumes: nothing (pure doc edit)
- Produces: the finalized backend contract that Tasks 2-4 write frontend code against — specifically, confirms `GET /api/v1/devices` (JWT admin) exists and that `POST /api/v1/auth/login` returns `{ token: string }` in the response body.

- [ ] **Step 1: Fix the Authentication section (§5)**

Use the Edit tool on `docs/superpowers/specs/2026-08-12-backend-csharp-design.md`:

old_string:
```
- **Dashboard admin:** username/password (BCrypt-hashed) checked against `admin_users`, issues a JWT via `Microsoft.AspNetCore.Authentication.JwtBearer`, cookie-based session.
```

new_string:
```
- **Dashboard admin:** username/password (BCrypt-hashed) checked against `admin_users`; `POST /api/v1/auth/login` issues a JWT via `Microsoft.AspNetCore.Authentication.JwtBearer`, returned in the JSON response body as `{ token: string }`. The frontend stores the token and sends it as `Authorization: Bearer <token>` on subsequent requests — no cookie is used.
```

- [ ] **Step 2: Add the `GET /api/v1/devices` row to the API Endpoints table (§6)**

old_string:
```
| `GET` | `/api/v1/locations/{deviceId}` | JWT | Unchanged |
| `GET` | `/api/v1/health` | None | Unchanged |
```

new_string:
```
| `GET` | `/api/v1/locations/{deviceId}` | JWT | Unchanged |
| `GET` | `/api/v1/devices` | JWT (admin) | **New** — latest location per device, backed by `usp_Location_GetLatestByDevice`; powers the devices list dashboard page |
| `GET` | `/api/v1/health` | None | Unchanged |
```

- [ ] **Step 3: Verify no stale "cookie-based session" wording remains**

Run: `grep -n "cookie-based session" docs/superpowers/specs/2026-08-12-backend-csharp-design.md`
Expected: no output (no match)

Run: `grep -n "GET.*api/v1/devices.*JWT (admin)" docs/superpowers/specs/2026-08-12-backend-csharp-design.md`
Expected: one match — the new endpoint row

- [ ] **Step 4: Commit**

```bash
git add docs/superpowers/specs/2026-08-12-backend-csharp-design.md
git commit -m "RT: fix auth wording, add GET /api/v1/devices to backend design"
```

---

### Task 2: Update `specs/frontend/pages.md` — add device registration page

**Files:**
- Modify: `specs/frontend/pages.md`

**Interfaces:**
- Consumes: `DeviceRegistrationRequest`, `DeviceRegistrationResult` types (defined in Task 4 — this task only references their names in prose/code samples, which is fine since these are docs, not compiled code)
- Consumes: `api.registerDevice(deviceId, displayName)` (defined in Task 3)
- Produces: `/devices/new` route and `AddDevicePage` component spec, referenced by no later task

- [ ] **Step 1: Add the new route to the Routes table**

old_string:
```
| `/` or `/login` | `LoginPage` | Admin JWT login |
| `/devices` | `DevicesPage` | List all devices with latest location + status |
| `/devices/:deviceId` | `DeviceDetailPage` | Detailed GPS data for one device |
```

new_string:
```
| `/` or `/login` | `LoginPage` | Admin JWT login |
| `/devices` | `DevicesPage` | List all devices with latest location + status |
| `/devices/new` | `AddDevicePage` | Register a new device |
| `/devices/:deviceId` | `DeviceDetailPage` | Detailed GPS data for one device |
```

- [ ] **Step 2: Add the "+ Add Device" button to DevicesPage's UI Requirements**

old_string:
```
### UI Requirements
- Table/list of devices
- Columns: Device ID, Last Timestamp, Latitude, Longitude, Status
- Status derived from `is_stale` and timestamp age (e.g., >60s = offline)
- Stale indicator: visual warning when `is_stale === true`
```

new_string:
```
### UI Requirements
- Table/list of devices
- Columns: Device ID, Last Timestamp, Latitude, Longitude, Status
- Status derived from `is_stale` and timestamp age (e.g., >60s = offline)
- Stale indicator: visual warning when `is_stale === true`
- "+ Add Device" button, links to `/devices/new`
```

- [ ] **Step 3: Insert the AddDevicePage section before DeviceDetailPage**

old_string:
```
## DeviceDetailPage

### Data Shape
```

new_string:
```
## AddDevicePage

### Data Shape
```typescript
const mutation = useMutation<DeviceRegistrationResult, Error, DeviceRegistrationRequest>({
  mutationFn: (req) => api.registerDevice(req.deviceId, req.displayName),
});
```

### UI Requirements
- Form fields: Device ID (required, text input), Display Name (optional, text input)
- Submit calls `api.registerDevice(deviceId, displayName)`
- On success: display the returned `apiKey` once, with a persistent warning that it will not be shown again (e.g., "Copy this now — you won't be able to see it again")
- After acknowledgment, link back to `/devices`
- On error (e.g., duplicate Device ID): show the error message inline, keep the form filled in

## DeviceDetailPage

### Data Shape
```

- [ ] **Step 4: Update the Notes section**

old_string:
```
## Notes
- Phase 1: no edit/create/delete flows
- Phase 1: no WebSockets; use polling
- Phase 1: no maps
```

new_string:
```
## Notes
- Phase 1: device registration only (no edit/delete); locations remain read-only, no create/edit UI for location data
- Phase 1: no WebSockets; use polling
- Phase 1: no maps
```

- [ ] **Step 5: Verify**

Run: `grep -n "AddDevicePage\|devices/new" specs/frontend/pages.md`
Expected: 3 matches (Routes table, DevicesPage button, AddDevicePage section header)

Run: `grep -n "no edit/create/delete" specs/frontend/pages.md`
Expected: no output (old wording removed)

- [ ] **Step 6: Commit**

```bash
git add specs/frontend/pages.md
git commit -m "RT: add AddDevicePage spec for device registration"
```

---

### Task 3: Update `specs/frontend/services.md` — implement TBD methods, add registerDevice, fix typo

**Files:**
- Modify: `specs/frontend/services.md`

**Interfaces:**
- Consumes: `GET /api/v1/devices`, `POST /api/v1/auth/login`, `POST /api/v1/devices` (all confirmed in Task 1)
- Consumes: `DeviceRegistrationResult` type (defined in Task 4)
- Produces: `ApiClient.getDevicesSummary(): Promise<DeviceSummary[]>`, `ApiClient.login(username, password): Promise<AuthState>`, `ApiClient.registerDevice(deviceId, displayName?): Promise<DeviceRegistrationResult>` — names/signatures referenced by Task 2's `AddDevicePage` spec

- [ ] **Step 1: Fix the `authHeader`/`authHeaders` typo in `getLatestLocation`**

old_string:
```
  async getLatestLocation(deviceId: string): Promise<Location> {
    const res = await fetch(`${this.baseUrl}/api/v1/locations/${deviceId}`, {
      headers: this.authHeader(),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  }
```

new_string:
```
  async getLatestLocation(deviceId: string): Promise<Location> {
    const res = await fetch(`${this.baseUrl}/api/v1/locations/${deviceId}`, {
      headers: this.authHeaders(),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  }
```

- [ ] **Step 2: Implement `getDevicesSummary`**

old_string:
```
  async getDevicesSummary(): Promise<DeviceSummary[]> {
    // TBD based on available backend endpoint
    // Could be a dedicated summary endpoint or client-side aggregation
    throw new Error("Not implemented: requires backend summary endpoint");
  }
```

new_string:
```
  async getDevicesSummary(): Promise<DeviceSummary[]> {
    const res = await fetch(`${this.baseUrl}/api/v1/devices`, {
      headers: this.authHeaders(),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  }
```

- [ ] **Step 3: Implement `login` and add `registerDevice`**

old_string:
```
  async login(username: string, password: string): Promise<AuthState> {
    // TBD exact JWT login flow (session cookie vs token response)
    throw new Error("Not implemented: TBD auth flow");
  }
```

new_string:
```
  async login(username: string, password: string): Promise<AuthState> {
    const res = await fetch(`${this.baseUrl}/api/v1/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password }),
    });
    if (!res.ok) throw new Error(await res.text());
    const { token }: { token: string } = await res.json();
    this.setToken(token);
    return { isAuthenticated: true, token };
  }

  async registerDevice(deviceId: string, displayName?: string): Promise<DeviceRegistrationResult> {
    const res = await fetch(`${this.baseUrl}/api/v1/devices`, {
      method: "POST",
      headers: { "Content-Type": "application/json", ...this.authHeaders() },
      body: JSON.stringify({ deviceId, displayName }),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  }
```

- [ ] **Step 4: Verify**

Run: `grep -n "TBD\|Not implemented" specs/frontend/services.md`
Expected: no output (both TBD methods now implemented)

Run: `grep -n "authHeader()" specs/frontend/services.md`
Expected: no output (typo fixed — only `authHeaders()` should remain)

Run: `grep -n "registerDevice" specs/frontend/services.md`
Expected: 2 matches (method definition, fetch call target reads `/api/v1/devices` — confirm by eye that method body is present)

- [ ] **Step 5: Commit**

```bash
git add specs/frontend/services.md
git commit -m "RT: implement getDevicesSummary/login, add registerDevice, fix authHeaders typo"
```

---

### Task 4: Update `specs/frontend/types.md` — add device registration types

**Files:**
- Modify: `specs/frontend/types.md`

**Interfaces:**
- Consumes: nothing
- Produces: `DeviceRegistrationRequest { deviceId: string; displayName?: string }`, `DeviceRegistrationResult { deviceId: string; apiKey: string }` — consumed by Task 2 (`AddDevicePage`) and Task 3 (`registerDevice`)

- [ ] **Step 1: Add the new types after the Auth section**

old_string:
```
## Auth (JWT session)

```typescript
export interface AuthState {
  isAuthenticated: boolean;
  token?: string;
}
```
```

new_string:
```
## Auth (JWT session)

```typescript
export interface AuthState {
  isAuthenticated: boolean;
  token?: string;
}
```

## Device Registration

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
```

- [ ] **Step 2: Verify**

Run: `grep -n "DeviceRegistrationRequest\|DeviceRegistrationResult" specs/frontend/types.md`
Expected: 4 matches (2 interface declarations + 2 usages inside their own bodies don't count — expect exactly the 2 `export interface` lines plus this section header context; confirm both interfaces are present with `deviceId`, `displayName?`, and `apiKey` fields)

- [ ] **Step 3: Commit**

```bash
git add specs/frontend/types.md
git commit -m "RT: add DeviceRegistrationRequest/Result types"
```

---

### Task 5: Close the GET /api/v1/devices gap in the implementation-facing specs

**Files:**
- Modify: `specs/backend/api.md`
- Modify: `specs/spec.md:280-287`
- Modify: `specs/frontend/services.md`

**Interfaces:**
- Consumes: `Location`, `DeviceSummary` types (already defined in `specs/frontend/types.md`)
- Produces: `deriveDeviceStatus(location: Location): "online" | "offline" | "stale"` helper in `services.md`, used by `getDevicesSummary`
- Context: the final whole-branch review found that Task 1 added `GET /api/v1/devices` only to the decision-record doc (`2026-08-12-backend-csharp-design.md`), not to `specs/backend/api.md` (the endpoint table an implementer actually builds from) or `specs/spec.md` §6.4. It also found `specs/backend/api.md:11` says `GET /api/v1/locations/{deviceId}` returns `LocationReadDto[]` ("0 or 1 item"), while the frontend spec has always treated it as a single `Location` — this task resolves that in favor of a single nullable object with `404`, matching the frontend as-is (no frontend change needed for that part). For `GET /api/v1/devices`, the human partner decided the backend returns a flat array of latest locations (reusing the existing `LocationReadDto`, backed by `usp_Location_GetLatestByDevice`) and the frontend derives `online`/`offline`/`stale` status client-side — this task adds that mapping step to `services.md`.

- [ ] **Step 1: Fix the `GET /api/v1/locations/{deviceId}` row and add the `GET /api/v1/devices` row in `specs/backend/api.md`'s endpoint table**

old_string:
```
| Method | Path | Auth | Body | Response | Purpose |
|--------|------|------|------|----------|---------|
| `POST` | `/api/v1/auth/login` | None | `LoginRequestDto` | `LoginResponseDto` | Admin login, issues JWT |
| `POST` | `/api/v1/devices` | JWT | `DeviceRegisterRequestDto` | `DeviceRegisterResponseDto` | Register a device, returns its API key once |
| `POST` | `/api/v1/locations` | `X-API-Key` | `LocationCreateDto` | `LocationCreateResponseDto` | Single upload |
| `POST` | `/api/v1/locations/batch` | `X-API-Key` | `LocationBatchCreateDto` | `LocationCreateResponseDto[]` | Batch upload |
| `GET` | `/api/v1/locations/{deviceId}` | JWT | `-` | `LocationReadDto[]` | Latest location for dashboard (0 or 1 item) |
| `GET` | `/api/v1/health` | None | `-` | `{"status": "ok"}` | Health/connectivity |
```

new_string:
```
| Method | Path | Auth | Body | Response | Purpose |
|--------|------|------|------|----------|---------|
| `POST` | `/api/v1/auth/login` | None | `LoginRequestDto` | `LoginResponseDto` | Admin login, issues JWT |
| `POST` | `/api/v1/devices` | JWT | `DeviceRegisterRequestDto` | `DeviceRegisterResponseDto` | Register a device, returns its API key once |
| `POST` | `/api/v1/locations` | `X-API-Key` | `LocationCreateDto` | `LocationCreateResponseDto` | Single upload |
| `POST` | `/api/v1/locations/batch` | `X-API-Key` | `LocationBatchCreateDto` | `LocationCreateResponseDto[]` | Batch upload |
| `GET` | `/api/v1/locations/{deviceId}` | JWT | `-` | `LocationReadDto` | Latest location for one device; `404` if none recorded yet |
| `GET` | `/api/v1/devices` | JWT | `-` | `LocationReadDto[]` | Latest location per device, one row per registered device (dashboard list) |
| `GET` | `/api/v1/health` | None | `-` | `{"status": "ok"}` | Health/connectivity |
```

- [ ] **Step 2: Add example contracts for both GET endpoints in `specs/backend/api.md`**

old_string:
```
**Error:** `401` on wrong username/password (standard envelope, `"error": "INVALID_CREDENTIALS"`).

## Notes
```

new_string:
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

**Error:** `404` (`"error": "NOT_FOUND"`) if the device has never reported a location. `401` (missing/invalid JWT).

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

One `LocationReadDto` per registered device (its most recent location), backed by `usp_Location_GetLatestByDevice`. The dashboard derives online/offline/stale status client-side from `isStale` and timestamp age — see `specs/frontend/pages.md`.

**Error:** `401` (missing/invalid JWT).

## Notes
```

- [ ] **Step 3: Add the matching row to `specs/spec.md`'s §6.4 endpoint table**

old_string:
```
| Method | Endpoint | Purpose | Auth |
|--------|----------|---------|------|
| `POST` | `/api/v1/auth/login` | Admin login, issues JWT | None |
| `POST` | `/api/v1/devices` | Register a device, returns its API key once | JWT |
| `POST` | `/api/v1/locations` | Single location upload | Device API key |
| `POST` | `/api/v1/locations/batch` | Batch upload (reconnection scenarios) | Device API key |
| `GET` | `/api/v1/locations/{deviceId}` | Latest location for dashboard | JWT |
| `GET` | `/api/v1/health` | Health check / connectivity verification | None |
```

new_string:
```
| Method | Endpoint | Purpose | Auth |
|--------|----------|---------|------|
| `POST` | `/api/v1/auth/login` | Admin login, issues JWT | None |
| `POST` | `/api/v1/devices` | Register a device, returns its API key once | JWT |
| `POST` | `/api/v1/locations` | Single location upload | Device API key |
| `POST` | `/api/v1/locations/batch` | Batch upload (reconnection scenarios) | Device API key |
| `GET` | `/api/v1/locations/{deviceId}` | Latest location for dashboard | JWT |
| `GET` | `/api/v1/devices` | Latest location per device (dashboard list view) | JWT |
| `GET` | `/api/v1/health` | Health check / connectivity verification | None |
```

- [ ] **Step 4: Add the `deriveDeviceStatus` helper to `specs/frontend/services.md`**

old_string:
```
```typescript
class ApiClient {
```

new_string:
```
```typescript
function deriveDeviceStatus(location: Location): "online" | "offline" | "stale" {
  if (location.isStale) return "stale";
  const ageMs = Date.now() - new Date(location.timestamp).getTime();
  return ageMs > 60_000 ? "offline" : "online";
}

class ApiClient {
```

- [ ] **Step 5: Update `getDevicesSummary` in `specs/frontend/services.md` to map the flat location array into `DeviceSummary[]`**

old_string:
```
  async getDevicesSummary(): Promise<DeviceSummary[]> {
    const res = await fetch(`${this.baseUrl}/api/v1/devices`, {
      headers: this.authHeaders(),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  }
```

new_string:
```
  async getDevicesSummary(): Promise<DeviceSummary[]> {
    const res = await fetch(`${this.baseUrl}/api/v1/devices`, {
      headers: this.authHeaders(),
    });
    if (!res.ok) throw new Error(await res.text());
    const locations: Location[] = await res.json();
    return locations.map((latest) => ({
      deviceId: latest.deviceId,
      latest,
      status: deriveDeviceStatus(latest),
    }));
  }
```

- [ ] **Step 6: Verify**

Run: `grep -n "GET.*api/v1/devices" specs/backend/api.md`
Expected: 1 match (the new endpoint row)

Run: `grep -cn "LocationReadDto\[\]" specs/backend/api.md`
Expected: `2` (batch-upload response row + the new devices-list row; the `/locations/{deviceId}` row is now singular `LocationReadDto` with no brackets)

Run: `grep -n "GET.*api/v1/devices" specs/spec.md`
Expected: 1 match

Run: `grep -cn "deriveDeviceStatus" specs/frontend/services.md`
Expected: `2` (function definition + one call site)

- [ ] **Step 7: Commit**

```bash
git add specs/backend/api.md specs/spec.md specs/frontend/services.md
git commit -m "RT: add GET /api/v1/devices to implementation specs, derive status client-side"
```

---

### Task 6: Fix stale summary sentences and TBD left behind by Tasks 1 and the original spec

**Files:**
- Modify: `docs/superpowers/specs/2026-08-12-backend-csharp-design.md`
- Modify: `specs/spec.md:635`

**Interfaces:**
- Consumes: nothing
- Produces: nothing consumed by later tasks — pure documentation-consistency cleanup
- Context: the final whole-branch review found two sentences in the backend design doc that summarized the API/frontend contract as "unchanged," which stopped being true once Task 1 added new endpoints and Tasks 2-4 changed three frontend files. It also found `specs/spec.md`'s open-TBDs table still poses the auth-persistence question as "session cookie vs. localStorage," even though the branch's own auth decision (`2026-08-12-backend-csharp-design.md` §5, from Task 1) already ruled out cookies.

- [ ] **Step 1: Fix the §1 Scope summary in the backend design doc**

old_string:
```
**Scope:** Backend only. Firmware (C++/ESP-IDF) and frontend (React/TypeScript) are unchanged. The JSON API contracts (camelCase field names, endpoint shapes) stay identical so neither of those layers needs to change.
```

new_string:
```
**Scope:** Backend only. Firmware (C++/ESP-IDF) is unchanged. Frontend (React/TypeScript) required updates for the new endpoints below — see `docs/superpowers/specs/2026-08-14-frontend-backend-alignment-design.md`. Existing endpoints keep camelCase field names and unchanged shapes; firmware integrations require no changes.
```

- [ ] **Step 2: Fix the §6 closing sentence in the backend design doc**

old_string:
```
Request/response JSON bodies (camelCase field names, shapes) are unchanged from the original spec — firmware and frontend integrations require no changes.
```

new_string:
```
Request/response JSON bodies (camelCase field names, shapes) for the pre-existing endpoints (`/api/v1/locations`, `/api/v1/locations/batch`, `/api/v1/health`) are unchanged from the original spec — firmware integrations require no changes there. The endpoints marked **New** above required matching frontend spec updates — see `docs/superpowers/specs/2026-08-14-frontend-backend-alignment-design.md`.
```

- [ ] **Step 3: Narrow the stale auth-persistence TBD in `specs/spec.md`**

old_string:
```
| **Dashboard auth persistence** | TBD (Session cookie vs. localStorage JWT) |
```

new_string:
```
| **Dashboard auth persistence** | TBD (localStorage vs. in-memory JWT storage — session-cookie approach ruled out, see `docs/superpowers/specs/2026-08-12-backend-csharp-design.md` §5) |
```

- [ ] **Step 4: Verify**

Run: `grep -n "frontend (React/TypeScript) are unchanged" docs/superpowers/specs/2026-08-12-backend-csharp-design.md`
Expected: no output

Run: `grep -n "firmware and frontend integrations require no changes" docs/superpowers/specs/2026-08-12-backend-csharp-design.md`
Expected: no output

Run: `grep -n "Session cookie vs. localStorage JWT" specs/spec.md`
Expected: no output

- [ ] **Step 5: Commit**

```bash
git add docs/superpowers/specs/2026-08-12-backend-csharp-design.md specs/spec.md
git commit -m "RT: fix stale unchanged-contract sentences and auth-persistence TBD"
```

---

### Task 7: Minor cleanups — snake_case leftover, endpoint table grouping, LoginPage spec gap

**Files:**
- Modify: `specs/frontend/pages.md`
- Modify: `docs/superpowers/specs/2026-08-12-backend-csharp-design.md`

**Interfaces:**
- Consumes: `AuthState` type (from `specs/frontend/types.md`), `api.login` (from `specs/frontend/services.md`)
- Produces: nothing consumed by later tasks
- Context: three Minor findings from the final whole-branch review, bundled since each is small: (1) `pages.md` still says `is_stale`/`is_stale === true` though `types.md` defines `isStale` (Task 3 already fixed the equivalent `authHeader`/`authHeaders` typo elsewhere, so this is the same class of leftover); (2) the backend design doc's endpoint table has its two `/api/v1/devices` rows separated by the health-check row; (3) `pages.md` documents `DevicesPage`, `AddDevicePage`, and `DeviceDetailPage` in full but `LoginPage` — the route this whole auth flow was built around — has no Data Shape / UI Requirements section.

- [ ] **Step 1: Fix `is_stale` → `isStale` in `specs/frontend/pages.md`**

old_string:
```
- Status derived from `is_stale` and timestamp age (e.g., >60s = offline)
- Stale indicator: visual warning when `is_stale === true`
- "+ Add Device" button, links to `/devices/new`
```

new_string:
```
- Status derived from `isStale` and timestamp age (e.g., >60s = offline)
- Stale indicator: visual warning when `isStale === true`
- "+ Add Device" button, links to `/devices/new`
```

- [ ] **Step 2: Group the two `/api/v1/devices` rows together in the backend design doc's endpoint table**

old_string:
```
| `POST` | `/api/v1/locations` | API key | Unchanged from original spec |
| `POST` | `/api/v1/locations/batch` | API key | Unchanged |
| `GET` | `/api/v1/locations/{deviceId}` | JWT | Unchanged |
| `GET` | `/api/v1/devices` | JWT (admin) | **New** — latest location per device, backed by `usp_Location_GetLatestByDevice`; powers the devices list dashboard page |
| `GET` | `/api/v1/health` | None | Unchanged |
| `POST` | `/api/v1/devices` | JWT (admin) | **New** — device registration, required now that `locations.device_fk` has a real FK constraint |
| `POST` | `/api/v1/auth/login` | None | **New** — explicit login endpoint issuing the JWT (implicit/unspecified in the original spec) |
```

new_string:
```
| `POST` | `/api/v1/locations` | API key | Unchanged from original spec |
| `POST` | `/api/v1/locations/batch` | API key | Unchanged |
| `GET` | `/api/v1/locations/{deviceId}` | JWT | Unchanged |
| `GET` | `/api/v1/health` | None | Unchanged |
| `POST` | `/api/v1/devices` | JWT (admin) | **New** — device registration, required now that `locations.device_fk` has a real FK constraint |
| `GET` | `/api/v1/devices` | JWT (admin) | **New** — latest location per device, backed by `usp_Location_GetLatestByDevice`; powers the devices list dashboard page |
| `POST` | `/api/v1/auth/login` | None | **New** — explicit login endpoint issuing the JWT (implicit/unspecified in the original spec) |
```

- [ ] **Step 3: Add a `LoginPage` section to `specs/frontend/pages.md`**

old_string:
```
| `/devices/:deviceId` | `DeviceDetailPage` | Detailed GPS data for one device |

## DevicesPage
```

new_string:
```
| `/devices/:deviceId` | `DeviceDetailPage` | Detailed GPS data for one device |

## LoginPage

### Data Shape
```typescript
const mutation = useMutation<AuthState, Error, { username: string; password: string }>({
  mutationFn: ({ username, password }) => api.login(username, password),
});
```

### UI Requirements
- Form fields: Username, Password (both required, password masked)
- Submit calls `api.login(username, password)`
- On success: redirect to `/devices`
- On error (401): show "Invalid username or password" inline, keep username filled in

## DevicesPage
```

- [ ] **Step 4: Verify**

Run: `grep -cn "isStale" specs/frontend/pages.md`
Expected: `2`

Run: `grep -n "is_stale" specs/frontend/pages.md`
Expected: no output

Run: `grep -n -A1 "POST.*api/v1/devices" docs/superpowers/specs/2026-08-12-backend-csharp-design.md`
Expected: the line immediately after the `POST /api/v1/devices` row is the `GET /api/v1/devices` row

Run: `grep -n "LoginPage" specs/frontend/pages.md`
Expected: 3 matches (routes table, section header, and no others — confirms the section was added once)

- [ ] **Step 5: Commit**

```bash
git add specs/frontend/pages.md docs/superpowers/specs/2026-08-12-backend-csharp-design.md
git commit -m "RT: fix isStale casing, group devices endpoint rows, add LoginPage spec"
```

---

## Self-Review Notes

- **Spec coverage:** All three gaps from the design doc (§3) are covered — Task 1 covers the backend amendments (§2), Tasks 2-4 cover the frontend changes (§3: pages.md, services.md, types.md respectively).
- **Placeholder scan:** No TBD/TODO left in any task; Task 3 Step 4 explicitly greps to confirm the old TBD markers are gone.
- **Type consistency:** `DeviceRegistrationRequest`/`DeviceRegistrationResult` (Task 4) match the names and fields used in Task 2's `AddDevicePage` mutation and Task 3's `registerDevice` return type exactly.

## Post-Review Addendum (Tasks 5-7)

Added after the final whole-branch review of Tasks 1-4 found that the branch's stated goal — frontend and backend specs agreeing — was not fully closed: `GET /api/v1/devices` existed only in the decision-record design doc, not in the implementation-facing specs (`specs/backend/api.md`, `specs/spec.md`), and its response shape was described three contradictory ways. The human partner approved expanding this plan's scope (see the Global Constraints scope note) and decided: (1) the backend returns a flat `LocationReadDto[]`/`Location[]` for `GET /api/v1/devices`, reusing the existing DTO rather than introducing a new one; (2) the frontend derives `online`/`offline`/`stale` status client-side, matching the rule `pages.md` already documented. Task 5 implements that. Tasks 6-7 clean up the Important/Minor findings from the same review (stale "unchanged" sentences, a stale TBD, an `is_stale` casing leftover, endpoint table ordering, and a missing `LoginPage` spec section).

- **Spec coverage:** Task 5 closes Critical finding #1 and Important finding #2 (singular vs. array mismatch on `GET /api/v1/locations/{deviceId}`, resolved in favor of the frontend's existing singular-object assumption). Task 6 closes Important findings #3 and #4. Task 7 closes Minor findings #5, #6, and #7. Minor finding #8 was a note, not a defect — no task needed.
- **Placeholder scan:** No TBD/TODO introduced; Task 6 Step 3 narrows (rather than removes) the one legitimate remaining TBD in `specs/spec.md`, per the reviewer's own recommendation not to delete a still-open question.
- **Type consistency:** Task 5's `deriveDeviceStatus(location: Location)` and `getDevicesSummary`'s use of it match the `Location`/`DeviceSummary` shapes already defined in `specs/frontend/types.md` (untouched by this addendum) exactly — `latest: Location`, `status: DeviceSummary["status"]`.
