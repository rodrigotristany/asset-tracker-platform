# Frontend/Backend Spec Alignment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring `specs/frontend/*.md` and `docs/superpowers/specs/2026-08-12-backend-csharp-design.md` into agreement, per the decisions recorded in `docs/superpowers/specs/2026-08-14-frontend-backend-alignment-design.md`.

**Architecture:** This is a documentation-only change set — no application code exists yet. Each task edits one spec file (exact `old_string`/`new_string` blocks are given), verifies the edit with `grep`, and commits.

**Tech Stack:** Markdown spec files only. No build/test tooling involved.

## Global Constraints

- Doc-only change — do not create or modify any source code files (no `.cs`, `.ts`, `.tsx`, etc.), only the four `.md` files named below.
- JSON/API field names stay camelCase — existing convention in both specs, unchanged by this plan.
- Preserve existing markdown table formatting (pipe-aligned columns) in any table edited.
- Every task's edits must be applied with the Edit tool using the exact `old_string`/`new_string` blocks given — do not paraphrase.

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

## Self-Review Notes

- **Spec coverage:** All three gaps from the design doc (§3) are covered — Task 1 covers the backend amendments (§2), Tasks 2-4 cover the frontend changes (§3: pages.md, services.md, types.md respectively).
- **Placeholder scan:** No TBD/TODO left in any task; Task 3 Step 4 explicitly greps to confirm the old TBD markers are gone.
- **Type consistency:** `DeviceRegistrationRequest`/`DeviceRegistrationResult` (Task 4) match the names and fields used in Task 2's `AddDevicePage` mutation and Task 3's `registerDevice` return type exactly.
