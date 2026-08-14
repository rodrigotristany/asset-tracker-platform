# Frontend Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the React/TypeScript dashboard from scratch in `dashboard/`, implementing every route and behavior already specified in `specs/frontend/{pages,services,types}.md` against the real backend (`backend/`, already implemented and matching those same specs).

**Architecture:** A single-page Vite + React + TypeScript app. `src/types.ts` and `src/api.ts` are transcribed directly from the approved specs (`types.md`, `services.md`) and form the boundary to the backend. `src/store/authStore.ts` (Zustand) holds the one piece of cross-page client state — the JWT — per `specs/spec.md` §9's tech stack ("TanStack Query + Zustand"); every other piece of state is server state fetched via TanStack Query, per-page. Four page components under `src/pages/` map 1:1 to `pages.md`'s route table, wired together in `src/App.tsx` via React Router, with a `ProtectedRoute` wrapper gating everything behind `/login`.

**Tech Stack:** React 19 (satisfies the spec's "React 18+"), TypeScript, Vite, Tailwind CSS v4, TanStack Query v5, React Router v7, Zustand v5, Vitest + React Testing Library. All exact versions below were installed and validated (`npm run build`, `npx vitest run`) together in this session on 2026-08-14 — they are not guesses.

## Global Constraints

- **React Router v7, not v6.** `specs/spec.md` §7.1 says "React Router v6," but the entire 6.x line (and everything up to 7.17.0) carries two unpatched moderate CVEs (GHSA-wrjc-x8rr-h8h6, GHSA-337j-9hxr-rhxg — `npm audit` confirms 0 vulnerabilities at 7.18.2+). The library-mode API this app uses — `BrowserRouter`, `Routes`, `Route`, `Navigate`, `useNavigate`, `useParams` — is unchanged between v6 and v7. Task 1 updates `specs/spec.md` to reflect this.
- **Tailwind CSS v4's zero-config setup**, not the v3-style `tailwind.config.js`/`postcss.config.js` pair `specs/spec.md` §14's repository-structure listing implies. Tailwind is configured via the `@tailwindcss/vite` plugin plus a single `@import "tailwindcss";` line in CSS — no config file needed. Task 1 updates that listing.
- **Dashboard auth persistence: in-memory only** (Zustand store, no persist middleware) — resolves the open TBD in `specs/spec.md`. A page refresh logs the admin out. Task 3 implements this and updates the TBD line.
- JSON/API field names stay camelCase (existing convention, matches `specs/frontend/types.md` verbatim).
- Every page/store/service file gets a colocated `*.test.tsx`/`*.test.ts` (Vitest + React Testing Library). Mock only the true I/O boundary — the `../api` module (which itself wraps `fetch`) — never React Query or Zustand internals. Assert on rendered output and user-visible behavior.
- Out of scope for every task (per `specs/spec.md` §7.4 and `specs/frontend/pages.md`'s Notes): maps, WebSockets/real-time updates, historical path replay, multi-user roles, edit/delete flows for devices or locations.
- Backend base URL defaults to `http://localhost:5125` (the real dev port in `backend/AssetTracker.Api/Properties/launchSettings.json`), overridable via the `VITE_API_BASE_URL` env var — never hardcode a different default.

## File Structure

```
dashboard/
├── package.json, vite.config.ts, tsconfig*.json, index.html   (Task 1)
├── src/
│   ├── main.tsx, App.tsx, App.test.tsx, index.css, test-setup.ts   (Task 1)
│   ├── types.ts, api.ts, api.test.ts                                (Task 2)
│   ├── store/authStore.ts, authStore.test.ts                        (Task 3)
│   ├── components/ProtectedRoute.tsx, ProtectedRoute.test.tsx       (Task 3)
│   └── pages/
│       ├── LoginPage.tsx, LoginPage.test.tsx                        (Task 3)
│       ├── DevicesPage.tsx, DevicesPage.test.tsx                    (Task 4)
│       ├── AddDevicePage.tsx, AddDevicePage.test.tsx                (Task 5)
│       └── DeviceDetailPage.tsx, DeviceDetailPage.test.tsx          (Task 6)
```

Each page task also modifies `dashboard/src/App.tsx` to wire its route in, replacing a placeholder or adding a new `<Route>`.

---

### Task 1: Project scaffolding

**Files:**
- Create: `dashboard/package.json`
- Create: `dashboard/vite.config.ts`
- Create: `dashboard/tsconfig.json`
- Create: `dashboard/tsconfig.app.json`
- Create: `dashboard/tsconfig.node.json`
- Create: `dashboard/index.html`
- Create: `dashboard/src/main.tsx`
- Create: `dashboard/src/App.tsx`
- Create: `dashboard/src/App.test.tsx`
- Create: `dashboard/src/index.css`
- Create: `dashboard/src/test-setup.ts`
- Modify: `.gitignore` (repo root)
- Modify: `specs/spec.md:388` (React Router version)
- Modify: `specs/spec.md:592-597` (repository structure — drop `tailwind.config.js`)

**Interfaces:**
- Consumes: nothing.
- Produces: the whole dev/build/test toolchain every later task runs on (`npm run dev`, `npm run build`, `npm test`). `App.tsx` exports a default `App` component containing a `BrowserRouter`/`Routes` tree with two placeholder route elements (`/` → redirect to `/login`, `/login` → inline "Login" placeholder text, `/devices` → inline "Devices" placeholder text). Tasks 3-6 replace these placeholders one at a time.

- [ ] **Step 1: Create `dashboard/package.json`**

```json
{
  "name": "dashboard",
  "private": true,
  "version": "0.0.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "tsc -b && vite build",
    "test": "vitest run",
    "lint": "oxlint",
    "preview": "vite preview"
  },
  "dependencies": {
    "@tailwindcss/vite": "^4.3.3",
    "@tanstack/react-query": "^5.101.4",
    "react": "^19.2.8",
    "react-dom": "^19.2.8",
    "react-router-dom": "^7.18.2",
    "zustand": "^5.0.15"
  },
  "devDependencies": {
    "@testing-library/jest-dom": "^7.0.1",
    "@testing-library/react": "^16.3.2",
    "@testing-library/user-event": "^14.6.4",
    "@types/node": "^24.13.3",
    "@types/react": "^19.2.17",
    "@types/react-dom": "^19.2.3",
    "@vitejs/plugin-react": "^6.0.4",
    "jsdom": "^29.1.1",
    "oxlint": "^1.75.0",
    "tailwindcss": "^4.3.3",
    "typescript": "~6.0.2",
    "vite": "^8.2.0",
    "vitest": "^4.1.10"
  }
}
```

- [ ] **Step 2: Create `dashboard/vite.config.ts`**

```typescript
/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test-setup.ts'],
    globals: true,
  },
})
```

- [ ] **Step 3: Create the TypeScript project files**

`dashboard/tsconfig.json`:
```json
{
  "files": [],
  "references": [
    { "path": "./tsconfig.app.json" },
    { "path": "./tsconfig.node.json" }
  ]
}
```

`dashboard/tsconfig.app.json`:
```json
{
  "compilerOptions": {
    "tsBuildInfoFile": "./node_modules/.tmp/tsconfig.app.tsbuildinfo",
    "target": "es2023",
    "lib": ["ES2023", "DOM"],
    "module": "esnext",
    "types": ["vite/client"],
    "allowArbitraryExtensions": true,
    "skipLibCheck": true,

    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "verbatimModuleSyntax": true,
    "moduleDetection": "force",
    "noEmit": true,
    "jsx": "react-jsx",

    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "erasableSyntaxOnly": true,
    "noFallthroughCasesInSwitch": true
  },
  "include": ["src"]
}
```

`dashboard/tsconfig.node.json`:
```json
{
  "compilerOptions": {
    "tsBuildInfoFile": "./node_modules/.tmp/tsconfig.node.tsbuildinfo",
    "target": "es2023",
    "lib": ["ES2023"],
    "types": ["node"],
    "skipLibCheck": true,

    "module": "nodenext",
    "allowImportingTsExtensions": true,
    "verbatimModuleSyntax": true,
    "moduleDetection": "force",
    "noEmit": true,

    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "erasableSyntaxOnly": true,
    "noFallthroughCasesInSwitch": true
  },
  "include": ["vite.config.ts"]
}
```

- [ ] **Step 4: Create `dashboard/index.html`**

```html
<!doctype html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Asset Tracker Dashboard</title>
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.tsx"></script>
  </body>
</html>
```

- [ ] **Step 5: Create `dashboard/src/index.css`**

```css
@import "tailwindcss";
```

- [ ] **Step 6: Create `dashboard/src/test-setup.ts`**

```typescript
import '@testing-library/jest-dom/vitest'
```

- [ ] **Step 7: Create `dashboard/src/App.tsx`** (placeholder routes — Tasks 3-6 replace these one at a time)

```tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'

function LoginPagePlaceholder() {
  return <div>Login</div>
}

function DevicesPagePlaceholder() {
  return <div>Devices</div>
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<LoginPagePlaceholder />} />
        <Route path="/devices" element={<DevicesPagePlaceholder />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
```

- [ ] **Step 8: Create `dashboard/src/main.tsx`**

```tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import './index.css'
import App from './App.tsx'

const queryClient = new QueryClient()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  </StrictMode>,
)
```

- [ ] **Step 9: Create `dashboard/src/App.test.tsx`**

```tsx
import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import App from './App'

describe('App', () => {
  it('redirects / to the login page', () => {
    render(<App />)
    expect(screen.getByText('Login')).toBeInTheDocument()
  })
})
```

- [ ] **Step 10: Add dashboard entries to the root `.gitignore`**

old_string:
```
# .NET
backend/**/bin/
backend/**/obj/
backend/**/*.user
```

new_string:
```
# .NET
backend/**/bin/
backend/**/obj/
backend/**/*.user

# Node / dashboard
dashboard/node_modules/
dashboard/dist/
dashboard/dist-ssr/
dashboard/*.local
```

- [ ] **Step 11: Fix the React Router version in `specs/spec.md`**

old_string:
```
| **Routing** | React Router v6 |
```

new_string:
```
| **Routing** | React Router v7 (upgraded from the originally-specced v6 — the whole 6.x line carries two unpatched moderate CVEs, GHSA-wrjc-x8rr-h8h6 and GHSA-337j-9hxr-rhxg; the library-mode API used here, `BrowserRouter`/`Routes`/`Route`/`Navigate`/`useNavigate`/`useParams`, is unchanged between the two) |
```

- [ ] **Step 12: Fix the repository-structure listing in `specs/spec.md`**

old_string:
```
├── dashboard/             # React + TypeScript frontend
│   ├── src/
│   ├── package.json
│   ├── vite.config.ts
│   ├── tailwind.config.js
│   └── tsconfig.json
```

new_string:
```
├── dashboard/             # React + TypeScript frontend
│   ├── src/
│   ├── package.json
│   ├── vite.config.ts     # Tailwind v4 configured here via @tailwindcss/vite (no separate tailwind.config.js)
│   └── tsconfig.json
```

- [ ] **Step 13: Install dependencies and verify**

Run: `cd dashboard && npm install`
Expected: installs cleanly, creates `dashboard/package-lock.json`.

Run: `npm run build`
Expected: `tsc -b && vite build` succeeds, produces `dashboard/dist/`.

Run: `npm test`
Expected: `App.test.tsx`'s one test passes.

Run: `npm audit`
Expected: `found 0 vulnerabilities`.

- [ ] **Step 14: Commit**

```bash
git add dashboard/ .gitignore specs/spec.md
git commit -m "RT: scaffold dashboard project (Vite + React + TS + Tailwind v4 + TanStack Query + React Router v7)"
```

---

### Task 2: Types and API client

**Files:**
- Create: `dashboard/src/types.ts`
- Create: `dashboard/src/api.ts`
- Create: `dashboard/src/api.test.ts`
- Create: `dashboard/.env.example`

**Interfaces:**
- Consumes: nothing from Task 1 beyond the toolchain.
- Produces: types `Location`, `LocationRow`, `DeviceSummary`, `AuthState`, `DeviceRegistrationRequest`, `DeviceRegistrationResult` (all from `dashboard/src/types.ts`); `export class ApiClient` with methods `getLatestLocation(deviceId: string): Promise<Location>`, `getDevicesSummary(): Promise<DeviceSummary[]>`, `login(username: string, password: string): Promise<AuthState>`, `registerDevice(deviceId: string, displayName?: string): Promise<DeviceRegistrationResult>`, `setToken(token: string): void`; `export const api: ApiClient` (the shared singleton every page imports); `export function deriveDeviceStatus(location: Location): "online" | "offline" | "stale"`. Tasks 3-6 import `api` and these types by these exact names.

- [ ] **Step 1: Create `dashboard/src/types.ts`** (transcribed verbatim from `specs/frontend/types.md`)

```typescript
export interface Location {
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

// For DB rows (includes internal id if ever needed)
export interface LocationRow extends Location {
  id: number;
}

export interface DeviceSummary {
  deviceId: string;
  latest: Location;
  status: "online" | "offline" | "stale";
}

export interface AuthState {
  isAuthenticated: boolean;
  token?: string;
}

export interface DeviceRegistrationRequest {
  deviceId: string;
  displayName?: string;
}

export interface DeviceRegistrationResult {
  deviceId: string;
  apiKey: string; // shown once, never retrievable again
}
```

- [ ] **Step 2: Create `dashboard/src/api.ts`** (transcribed from `specs/frontend/services.md`, with `export` added so the module is importable, and a shared singleton exported at the bottom)

```typescript
import type {
  AuthState,
  DeviceRegistrationResult,
  DeviceSummary,
  Location,
} from "./types";

export function deriveDeviceStatus(location: Location): "online" | "offline" | "stale" {
  if (location.isStale) return "stale";
  const ageMs = Date.now() - new Date(location.timestamp).getTime();
  return ageMs > 60_000 ? "offline" : "online";
}

export class ApiClient {
  private baseUrl: string;
  private token?: string;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  setToken(token: string) {
    this.token = token;
  }

  async getLatestLocation(deviceId: string): Promise<Location> {
    const res = await fetch(`${this.baseUrl}/api/v1/locations/${deviceId}`, {
      headers: this.authHeaders(),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  }

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

  private authHeaders(): HeadersInit {
    if (!this.token) return {};
    return { Authorization: `Bearer ${this.token}` };
  }
}

export const api = new ApiClient(import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5125");
```

- [ ] **Step 3: Create `dashboard/.env.example`**

```
VITE_API_BASE_URL=http://localhost:5125
```

- [ ] **Step 4: Create `dashboard/src/api.test.ts`**

```typescript
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiClient } from "./api";
import type { Location } from "./types";

describe("ApiClient", () => {
  let client: ApiClient;

  beforeEach(() => {
    client = new ApiClient("http://test.local");
    vi.stubGlobal("fetch", vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("login stores the token and returns an authenticated AuthState", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify({ token: "jwt-abc" }), { status: 200 }),
    );

    const result = await client.login("admin", "secret");

    expect(result).toEqual({ isAuthenticated: true, token: "jwt-abc" });
    expect(fetch).toHaveBeenCalledWith(
      "http://test.local/api/v1/auth/login",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ username: "admin", password: "secret" }),
      }),
    );
  });

  it("login throws on a non-ok response", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(new Response("bad credentials", { status: 401 }));

    await expect(client.login("admin", "wrong")).rejects.toThrow("bad credentials");
  });

  it("sends the bearer token on authenticated requests after login", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify({ token: "jwt-abc" }), { status: 200 }),
    );
    await client.login("admin", "secret");

    vi.mocked(fetch).mockResolvedValueOnce(new Response(JSON.stringify([]), { status: 200 }));
    await client.getDevicesSummary();

    expect(fetch).toHaveBeenLastCalledWith(
      "http://test.local/api/v1/devices",
      expect.objectContaining({
        headers: { Authorization: "Bearer jwt-abc" },
      }),
    );
  });

  it("getDevicesSummary marks a device online when its latest location is fresh and not stale", async () => {
    const freshLocation: Location = {
      deviceId: "goat-001",
      timestamp: new Date().toISOString(),
      latitude: 1,
      longitude: 1,
      isStale: false,
    };
    vi.mocked(fetch).mockResolvedValueOnce(new Response(JSON.stringify([freshLocation]), { status: 200 }));

    const result = await client.getDevicesSummary();

    expect(result).toEqual([{ deviceId: "goat-001", latest: freshLocation, status: "online" }]);
  });

  it("getDevicesSummary marks a device offline when its latest location is older than 60 seconds", async () => {
    const staleAgeLocation: Location = {
      deviceId: "goat-002",
      timestamp: new Date(Date.now() - 61_000).toISOString(),
      latitude: 1,
      longitude: 1,
      isStale: false,
    };
    vi.mocked(fetch).mockResolvedValueOnce(new Response(JSON.stringify([staleAgeLocation]), { status: 200 }));

    const result = await client.getDevicesSummary();

    expect(result[0].status).toBe("offline");
  });

  it("getDevicesSummary marks a device stale when isStale is true, regardless of age", async () => {
    const staleFlagLocation: Location = {
      deviceId: "goat-003",
      timestamp: new Date().toISOString(),
      latitude: 1,
      longitude: 1,
      isStale: true,
    };
    vi.mocked(fetch).mockResolvedValueOnce(new Response(JSON.stringify([staleFlagLocation]), { status: 200 }));

    const result = await client.getDevicesSummary();

    expect(result[0].status).toBe("stale");
  });

  it("registerDevice posts the device id and display name", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify({ deviceId: "goat-004", apiKey: "key-123" }), { status: 201 }),
    );

    const result = await client.registerDevice("goat-004", "Goat 004");

    expect(result).toEqual({ deviceId: "goat-004", apiKey: "key-123" });
    expect(fetch).toHaveBeenCalledWith(
      "http://test.local/api/v1/devices",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ deviceId: "goat-004", displayName: "Goat 004" }),
      }),
    );
  });

  it("getLatestLocation throws on a 404 response", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(new Response("not found", { status: 404 }));

    await expect(client.getLatestLocation("ghost-001")).rejects.toThrow("not found");
  });
});
```

- [ ] **Step 5: Run the tests**

Run (from `dashboard/`): `npm test`
Expected: all `api.test.ts` cases pass, plus `App.test.tsx` still passes.

- [ ] **Step 6: Commit**

```bash
git add dashboard/src/types.ts dashboard/src/api.ts dashboard/src/api.test.ts dashboard/.env.example
git commit -m "RT: add TypeScript types and ApiClient service, transcribed from specs/frontend"
```

---

### Task 3: Auth store, LoginPage, ProtectedRoute

**Files:**
- Create: `dashboard/src/store/authStore.ts`
- Create: `dashboard/src/store/authStore.test.ts`
- Create: `dashboard/src/pages/LoginPage.tsx`
- Create: `dashboard/src/pages/LoginPage.test.tsx`
- Create: `dashboard/src/components/ProtectedRoute.tsx`
- Create: `dashboard/src/components/ProtectedRoute.test.tsx`
- Modify: `dashboard/src/App.tsx`
- Modify: `dashboard/src/App.test.tsx`

**Interfaces:**
- Consumes: `api` singleton, `AuthState` type (Task 2).
- Produces: `export const useAuthStore` (Zustand hook) exposing `{ isAuthenticated, token, login(username, password): Promise<void>, logout(): void }`; `export function ProtectedRoute({ children }: { children: ReactNode })`; `export function LoginPage()`. Tasks 4-6 wrap their routes in `<ProtectedRoute>` the same way this task wraps `/devices`.

- [ ] **Step 1: Create `dashboard/src/store/authStore.ts`**

```typescript
import { create } from "zustand";
import { api } from "../api";
import type { AuthState } from "../types";

interface AuthStore extends AuthState {
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}

export const useAuthStore = create<AuthStore>((set) => ({
  isAuthenticated: false,
  token: undefined,
  login: async (username, password) => {
    const authState = await api.login(username, password);
    set(authState);
  },
  logout: () => {
    set({ isAuthenticated: false, token: undefined });
  },
}));
```

- [ ] **Step 2: Create `dashboard/src/store/authStore.test.ts`**

```typescript
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useAuthStore } from "./authStore";
import { api } from "../api";

vi.mock("../api", () => ({
  api: { login: vi.fn() },
}));

describe("useAuthStore", () => {
  beforeEach(() => {
    useAuthStore.setState({ isAuthenticated: false, token: undefined });
    vi.mocked(api.login).mockReset();
  });

  it("starts unauthenticated", () => {
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });

  it("login updates the store from the API client's result", async () => {
    vi.mocked(api.login).mockResolvedValueOnce({ isAuthenticated: true, token: "jwt-abc" });

    await useAuthStore.getState().login("admin", "secret");

    expect(useAuthStore.getState()).toMatchObject({ isAuthenticated: true, token: "jwt-abc" });
    expect(api.login).toHaveBeenCalledWith("admin", "secret");
  });

  it("logout resets the store to unauthenticated", () => {
    useAuthStore.setState({ isAuthenticated: true, token: "jwt-abc" });

    useAuthStore.getState().logout();

    expect(useAuthStore.getState()).toMatchObject({ isAuthenticated: false, token: undefined });
  });
});
```

- [ ] **Step 3: Create `dashboard/src/pages/LoginPage.tsx`**

```tsx
import { useState, type FormEvent } from "react";
import { useMutation } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "../store/authStore";

export function LoginPage() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const navigate = useNavigate();
  const login = useAuthStore((state) => state.login);

  const mutation = useMutation<void, Error, { username: string; password: string }>({
    mutationFn: ({ username, password }) => login(username, password),
    onSuccess: () => navigate("/devices"),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    mutation.mutate({ username, password });
  };

  return (
    <form onSubmit={handleSubmit}>
      <h1>Sign in</h1>
      <label htmlFor="username">Username</label>
      <input
        id="username"
        value={username}
        onChange={(event) => setUsername(event.target.value)}
        required
      />
      <label htmlFor="password">Password</label>
      <input
        id="password"
        type="password"
        value={password}
        onChange={(event) => setPassword(event.target.value)}
        required
      />
      <button type="submit" disabled={mutation.isPending}>
        Sign in
      </button>
      {mutation.isError && <p role="alert">Invalid username or password</p>}
    </form>
  );
}
```

- [ ] **Step 4: Create `dashboard/src/pages/LoginPage.test.tsx`**

```tsx
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { LoginPage } from "./LoginPage";
import { useAuthStore } from "../store/authStore";
import { api } from "../api";

vi.mock("../api", () => ({
  api: { login: vi.fn() },
}));

function renderLoginPage() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={["/login"]}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/devices" element={<div>Devices Page</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("LoginPage", () => {
  beforeEach(() => {
    useAuthStore.setState({ isAuthenticated: false, token: undefined });
  });

  it("submits the entered credentials and redirects to /devices on success", async () => {
    vi.mocked(api.login).mockResolvedValueOnce({ isAuthenticated: true, token: "jwt-abc" });
    renderLoginPage();

    fireEvent.change(screen.getByLabelText("Username"), { target: { value: "admin" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "secret" } });
    fireEvent.click(screen.getByRole("button", { name: "Sign in" }));

    await waitFor(() => expect(api.login).toHaveBeenCalledWith("admin", "secret"));
    expect(await screen.findByText("Devices Page")).toBeInTheDocument();
  });

  it("shows an inline error and keeps the username filled in on failed login", async () => {
    vi.mocked(api.login).mockRejectedValueOnce(new Error("Invalid credentials"));
    renderLoginPage();

    fireEvent.change(screen.getByLabelText("Username"), { target: { value: "admin" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "wrong" } });
    fireEvent.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Invalid username or password");
    expect(screen.getByLabelText("Username")).toHaveValue("admin");
  });
});
```

- [ ] **Step 5: Create `dashboard/src/components/ProtectedRoute.tsx`**

```tsx
import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useAuthStore } from "../store/authStore";

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
}
```

- [ ] **Step 6: Create `dashboard/src/components/ProtectedRoute.test.tsx`**

```tsx
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import { beforeEach, describe, expect, it } from "vitest";
import { ProtectedRoute } from "./ProtectedRoute";
import { useAuthStore } from "../store/authStore";

function renderProtectedRoute() {
  return render(
    <MemoryRouter initialEntries={["/devices"]}>
      <Routes>
        <Route path="/login" element={<div>Login Page</div>} />
        <Route
          path="/devices"
          element={
            <ProtectedRoute>
              <div>Devices Page</div>
            </ProtectedRoute>
          }
        />
      </Routes>
    </MemoryRouter>,
  );
}

describe("ProtectedRoute", () => {
  beforeEach(() => {
    useAuthStore.setState({ isAuthenticated: false, token: undefined });
  });

  it("redirects to /login when not authenticated", () => {
    renderProtectedRoute();
    expect(screen.getByText("Login Page")).toBeInTheDocument();
  });

  it("renders its children when authenticated", () => {
    useAuthStore.setState({ isAuthenticated: true, token: "jwt-abc" });
    renderProtectedRoute();
    expect(screen.getByText("Devices Page")).toBeInTheDocument();
  });
});
```

- [ ] **Step 7: Wire `LoginPage` and `ProtectedRoute` into `App.tsx`**

old_string:
```tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'

function LoginPagePlaceholder() {
  return <div>Login</div>
}

function DevicesPagePlaceholder() {
  return <div>Devices</div>
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<LoginPagePlaceholder />} />
        <Route path="/devices" element={<DevicesPagePlaceholder />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
```

new_string:
```tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { ProtectedRoute } from './components/ProtectedRoute'

function DevicesPagePlaceholder() {
  return <div>Devices</div>
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/devices"
          element={
            <ProtectedRoute>
              <DevicesPagePlaceholder />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  )
}

export default App
```

- [ ] **Step 8: Update `App.test.tsx`** for the now-real `LoginPage` (needs a `QueryClientProvider` since `LoginPage` uses `useMutation`)

old_string:
```tsx
import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import App from './App'

describe('App', () => {
  it('redirects / to the login page', () => {
    render(<App />)
    expect(screen.getByText('Login')).toBeInTheDocument()
  })
})
```

new_string:
```tsx
import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { describe, expect, it } from 'vitest'
import App from './App'

describe('App', () => {
  it('redirects / to the login page', () => {
    const queryClient = new QueryClient()
    render(
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>,
    )
    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
  })
})
```

- [ ] **Step 9: Resolve the auth-persistence TBD in `specs/spec.md`**

old_string:
```
| **Dashboard auth persistence** | TBD (localStorage vs. in-memory JWT storage — session-cookie approach ruled out, see `docs/superpowers/specs/2026-08-12-backend-csharp-design.md` §5) |
```

new_string:
```
| **Dashboard auth persistence** | Resolved — in-memory only (Zustand store, no persist middleware). A page refresh logs the admin out; avoids storing the JWT in localStorage where it would be readable by any injected script. See `dashboard/src/store/authStore.ts`. |
```

- [ ] **Step 10: Run the tests**

Run (from `dashboard/`): `npm test`
Expected: all tests pass, including the four new files and the updated `App.test.tsx`.

- [ ] **Step 11: Commit**

```bash
git add dashboard/src/store dashboard/src/pages/LoginPage.tsx dashboard/src/pages/LoginPage.test.tsx dashboard/src/components dashboard/src/App.tsx dashboard/src/App.test.tsx specs/spec.md
git commit -m "RT: add Zustand auth store, LoginPage, and ProtectedRoute"
```

---

### Task 4: DevicesPage

**Files:**
- Create: `dashboard/src/pages/DevicesPage.tsx`
- Create: `dashboard/src/pages/DevicesPage.test.tsx`
- Modify: `dashboard/src/App.tsx`

**Interfaces:**
- Consumes: `api.getDevicesSummary()`, `DeviceSummary` type (Task 2); `ProtectedRoute` (Task 3).
- Produces: `export function DevicesPage()`. No later task imports it directly, but Task 5's "+ Add Device" link target (`/devices/new`) and Task 6's per-row link target (`/devices/:deviceId`) must match the routes this task links to.

- [ ] **Step 1: Create `dashboard/src/pages/DevicesPage.tsx`**

```tsx
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { api } from "../api";
import type { DeviceSummary } from "../types";

export function DevicesPage() {
  const { data, isLoading, isError } = useQuery<DeviceSummary[]>({
    queryKey: ["devices"],
    queryFn: () => api.getDevicesSummary(),
    refetchInterval: 5000,
  });

  return (
    <div>
      <h1>Devices</h1>
      <Link to="/devices/new">+ Add Device</Link>
      {isLoading && <p>Loading…</p>}
      {isError && <p role="alert">Failed to load devices.</p>}
      {data && (
        <table>
          <thead>
            <tr>
              <th>Device ID</th>
              <th>Last Timestamp</th>
              <th>Latitude</th>
              <th>Longitude</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {data.map((device) => (
              <tr key={device.deviceId}>
                <td>
                  <Link to={`/devices/${device.deviceId}`}>{device.deviceId}</Link>
                </td>
                <td>{device.latest.timestamp}</td>
                <td>{device.latest.latitude}</td>
                <td>{device.latest.longitude}</td>
                <td>
                  {device.status}
                  {device.latest.isStale && (
                    <span role="img" aria-label="stale warning">
                      {" "}
                      ⚠️
                    </span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
```

- [ ] **Step 2: Create `dashboard/src/pages/DevicesPage.test.tsx`**

```tsx
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { DevicesPage } from "./DevicesPage";
import { api } from "../api";
import type { DeviceSummary } from "../types";

vi.mock("../api", () => ({
  api: { getDevicesSummary: vi.fn() },
}));

function renderDevicesPage() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <DevicesPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("DevicesPage", () => {
  it("renders a row per device with its latest location and status", async () => {
    const devices: DeviceSummary[] = [
      {
        deviceId: "goat-001",
        latest: { deviceId: "goat-001", timestamp: "2026-08-14T12:00:00Z", latitude: 1, longitude: 2, isStale: false },
        status: "online",
      },
    ];
    vi.mocked(api.getDevicesSummary).mockResolvedValueOnce(devices);

    renderDevicesPage();

    expect(await screen.findByText("goat-001")).toBeInTheDocument();
    expect(screen.getByText("online")).toBeInTheDocument();
  });

  it("shows a stale warning indicator when isStale is true", async () => {
    const devices: DeviceSummary[] = [
      {
        deviceId: "goat-002",
        latest: { deviceId: "goat-002", timestamp: "2026-08-14T12:00:00Z", latitude: 1, longitude: 2, isStale: true },
        status: "stale",
      },
    ];
    vi.mocked(api.getDevicesSummary).mockResolvedValueOnce(devices);

    renderDevicesPage();

    expect(await screen.findByRole("img", { name: "stale warning" })).toBeInTheDocument();
  });

  it("has a link to register a new device", async () => {
    vi.mocked(api.getDevicesSummary).mockResolvedValueOnce([]);
    renderDevicesPage();

    expect(await screen.findByRole("link", { name: "+ Add Device" })).toHaveAttribute("href", "/devices/new");
  });

  it("links each device row to its detail page", async () => {
    const devices: DeviceSummary[] = [
      {
        deviceId: "goat-003",
        latest: { deviceId: "goat-003", timestamp: "2026-08-14T12:00:00Z", latitude: 1, longitude: 2, isStale: false },
        status: "online",
      },
    ];
    vi.mocked(api.getDevicesSummary).mockResolvedValueOnce(devices);

    renderDevicesPage();

    expect(await screen.findByRole("link", { name: "goat-003" })).toHaveAttribute("href", "/devices/goat-003");
  });
});
```

- [ ] **Step 3: Wire `DevicesPage` into `App.tsx`**

old_string:
```tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { ProtectedRoute } from './components/ProtectedRoute'

function DevicesPagePlaceholder() {
  return <div>Devices</div>
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/devices"
          element={
            <ProtectedRoute>
              <DevicesPagePlaceholder />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  )
}

export default App
```

new_string:
```tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { DevicesPage } from './pages/DevicesPage'
import { ProtectedRoute } from './components/ProtectedRoute'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/devices"
          element={
            <ProtectedRoute>
              <DevicesPage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  )
}

export default App
```

- [ ] **Step 4: Run the tests**

Run (from `dashboard/`): `npm test`
Expected: all tests pass, including the four new `DevicesPage` cases.

- [ ] **Step 5: Commit**

```bash
git add dashboard/src/pages/DevicesPage.tsx dashboard/src/pages/DevicesPage.test.tsx dashboard/src/App.tsx
git commit -m "RT: add DevicesPage"
```

---

### Task 5: AddDevicePage

**Files:**
- Create: `dashboard/src/pages/AddDevicePage.tsx`
- Create: `dashboard/src/pages/AddDevicePage.test.tsx`
- Modify: `dashboard/src/App.tsx`

**Interfaces:**
- Consumes: `api.registerDevice(deviceId, displayName?)`, `DeviceRegistrationResult` type (Task 2); `ProtectedRoute` (Task 3).
- Produces: `export function AddDevicePage()`. No later task imports it.

- [ ] **Step 1: Create `dashboard/src/pages/AddDevicePage.tsx`**

```tsx
import { useState, type FormEvent } from "react";
import { useMutation } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { api } from "../api";
import type { DeviceRegistrationResult } from "../types";

export function AddDevicePage() {
  const [deviceId, setDeviceId] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [result, setResult] = useState<DeviceRegistrationResult | null>(null);

  const mutation = useMutation<DeviceRegistrationResult, Error, { deviceId: string; displayName: string }>({
    mutationFn: (req) => api.registerDevice(req.deviceId, req.displayName || undefined),
    onSuccess: (data) => setResult(data),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    mutation.mutate({ deviceId, displayName });
  };

  if (result) {
    return (
      <div>
        <h1>Device registered</h1>
        <p role="alert">Copy this now — you won't be able to see it again</p>
        <code>{result.apiKey}</code>
        <p>
          <Link to="/devices">Back to devices</Link>
        </p>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit}>
      <h1>Add Device</h1>
      <label htmlFor="deviceId">Device ID</label>
      <input id="deviceId" value={deviceId} onChange={(event) => setDeviceId(event.target.value)} required />
      <label htmlFor="displayName">Display Name</label>
      <input id="displayName" value={displayName} onChange={(event) => setDisplayName(event.target.value)} />
      <button type="submit" disabled={mutation.isPending}>
        Register
      </button>
      {mutation.isError && <p role="alert">{mutation.error.message}</p>}
    </form>
  );
}
```

- [ ] **Step 2: Create `dashboard/src/pages/AddDevicePage.test.tsx`**

```tsx
import { render, screen, fireEvent } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { AddDevicePage } from "./AddDevicePage";
import { api } from "../api";

vi.mock("../api", () => ({
  api: { registerDevice: vi.fn() },
}));

function renderAddDevicePage() {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AddDevicePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("AddDevicePage", () => {
  it("shows the returned API key once with a persistent warning after successful registration", async () => {
    vi.mocked(api.registerDevice).mockResolvedValueOnce({ deviceId: "goat-005", apiKey: "key-xyz" });
    renderAddDevicePage();

    fireEvent.change(screen.getByLabelText("Device ID"), { target: { value: "goat-005" } });
    fireEvent.click(screen.getByRole("button", { name: "Register" }));

    expect(await screen.findByText("key-xyz")).toBeInTheDocument();
    expect(screen.getByText(/won't be able to see it again/)).toBeInTheDocument();
    expect(api.registerDevice).toHaveBeenCalledWith("goat-005", undefined);
  });

  it("sends the display name when provided", async () => {
    vi.mocked(api.registerDevice).mockResolvedValueOnce({ deviceId: "goat-006", apiKey: "key-abc" });
    renderAddDevicePage();

    fireEvent.change(screen.getByLabelText("Device ID"), { target: { value: "goat-006" } });
    fireEvent.change(screen.getByLabelText("Display Name"), { target: { value: "Goat 006" } });
    fireEvent.click(screen.getByRole("button", { name: "Register" }));

    await screen.findByText("key-abc");
    expect(api.registerDevice).toHaveBeenCalledWith("goat-006", "Goat 006");
  });

  it("shows an inline error and keeps the form filled in on failure", async () => {
    vi.mocked(api.registerDevice).mockRejectedValueOnce(new Error("Device ID already registered"));
    renderAddDevicePage();

    fireEvent.change(screen.getByLabelText("Device ID"), { target: { value: "goat-007" } });
    fireEvent.click(screen.getByRole("button", { name: "Register" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Device ID already registered");
    expect(screen.getByLabelText("Device ID")).toHaveValue("goat-007");
  });
});
```

- [ ] **Step 3: Add the `/devices/new` route to `App.tsx`**

old_string:
```tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { DevicesPage } from './pages/DevicesPage'
import { ProtectedRoute } from './components/ProtectedRoute'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/devices"
          element={
            <ProtectedRoute>
              <DevicesPage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  )
}

export default App
```

new_string:
```tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { DevicesPage } from './pages/DevicesPage'
import { AddDevicePage } from './pages/AddDevicePage'
import { ProtectedRoute } from './components/ProtectedRoute'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/devices"
          element={
            <ProtectedRoute>
              <DevicesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/devices/new"
          element={
            <ProtectedRoute>
              <AddDevicePage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  )
}

export default App
```

- [ ] **Step 4: Run the tests**

Run (from `dashboard/`): `npm test`
Expected: all tests pass, including the three new `AddDevicePage` cases.

- [ ] **Step 5: Commit**

```bash
git add dashboard/src/pages/AddDevicePage.tsx dashboard/src/pages/AddDevicePage.test.tsx dashboard/src/App.tsx
git commit -m "RT: add AddDevicePage"
```

---

### Task 6: DeviceDetailPage

**Files:**
- Create: `dashboard/src/pages/DeviceDetailPage.tsx`
- Create: `dashboard/src/pages/DeviceDetailPage.test.tsx`
- Modify: `dashboard/src/App.tsx`

**Interfaces:**
- Consumes: `api.getLatestLocation(deviceId)`, `Location` type (Task 2); `ProtectedRoute` (Task 3). Route path `/devices/:deviceId` must match the link Task 4's `DevicesPage` already renders (`/devices/${device.deviceId}`).
- Produces: `export function DeviceDetailPage()`. Last task in the plan — nothing consumes it further.

- [ ] **Step 1: Create `dashboard/src/pages/DeviceDetailPage.tsx`**

```tsx
import { useQuery } from "@tanstack/react-query";
import { useParams } from "react-router-dom";
import { api } from "../api";
import type { Location } from "../types";

export function DeviceDetailPage() {
  const { deviceId } = useParams<{ deviceId: string }>();

  const { data, isLoading, isError } = useQuery<Location>({
    queryKey: ["locations", deviceId],
    queryFn: () => api.getLatestLocation(deviceId!),
    refetchInterval: 2000,
    enabled: Boolean(deviceId),
  });

  if (isLoading) return <p>Loading…</p>;
  if (isError) return <p role="alert">Failed to load location for {deviceId}.</p>;
  if (!data) return null;

  return (
    <div>
      <h1>{deviceId}</h1>
      <dl>
        <dt>Timestamp</dt>
        <dd>{data.timestamp}</dd>
        <dt>Latitude</dt>
        <dd>{data.latitude}</dd>
        <dt>Longitude</dt>
        <dd>{data.longitude}</dd>
        {data.altitude !== undefined && (
          <>
            <dt>Altitude</dt>
            <dd>{data.altitude}</dd>
          </>
        )}
        {data.speed !== undefined && (
          <>
            <dt>Speed</dt>
            <dd>{data.speed}</dd>
          </>
        )}
        {data.satellites !== undefined && (
          <>
            <dt>Satellites</dt>
            <dd>{data.satellites}</dd>
          </>
        )}
        {data.hdop !== undefined && (
          <>
            <dt>HDOP</dt>
            <dd>{data.hdop}</dd>
          </>
        )}
      </dl>
      {data.batteryVoltage !== undefined && <p>Battery: {data.batteryVoltage}V</p>}
      {data.isStale && <p role="alert">Stale data warning</p>}
    </div>
  );
}
```

- [ ] **Step 2: Create `dashboard/src/pages/DeviceDetailPage.test.tsx`**

```tsx
import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import { DeviceDetailPage } from "./DeviceDetailPage";
import { api } from "../api";
import type { Location } from "../types";

vi.mock("../api", () => ({
  api: { getLatestLocation: vi.fn() },
}));

function renderDeviceDetailPage(deviceId: string) {
  const queryClient = new QueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/devices/${deviceId}`]}>
        <Routes>
          <Route path="/devices/:deviceId" element={<DeviceDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("DeviceDetailPage", () => {
  it("renders the full GPS payload for the device", async () => {
    const location: Location = {
      deviceId: "goat-001",
      timestamp: "2026-08-14T12:00:00Z",
      latitude: 1.5,
      longitude: 2.5,
      altitude: 100,
      speed: 5,
      satellites: 9,
      hdop: 0.8,
      batteryVoltage: 3.7,
      isStale: false,
    };
    vi.mocked(api.getLatestLocation).mockResolvedValueOnce(location);

    renderDeviceDetailPage("goat-001");

    expect(await screen.findByText("1.5")).toBeInTheDocument();
    expect(screen.getByText(/3\.7V/)).toBeInTheDocument();
    expect(api.getLatestLocation).toHaveBeenCalledWith("goat-001");
  });

  it("shows a stale data warning when isStale is true", async () => {
    const location: Location = {
      deviceId: "goat-002",
      timestamp: "2026-08-14T12:00:00Z",
      latitude: 1,
      longitude: 2,
      isStale: true,
    };
    vi.mocked(api.getLatestLocation).mockResolvedValueOnce(location);

    renderDeviceDetailPage("goat-002");

    expect(await screen.findByText("Stale data warning")).toBeInTheDocument();
  });

  it("shows an error message when the location fails to load", async () => {
    vi.mocked(api.getLatestLocation).mockRejectedValueOnce(new Error("not found"));

    renderDeviceDetailPage("ghost-001");

    expect(await screen.findByRole("alert")).toHaveTextContent("Failed to load location");
  });
});
```

- [ ] **Step 3: Add the `/devices/:deviceId` route to `App.tsx`**

old_string:
```tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { DevicesPage } from './pages/DevicesPage'
import { AddDevicePage } from './pages/AddDevicePage'
import { ProtectedRoute } from './components/ProtectedRoute'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/devices"
          element={
            <ProtectedRoute>
              <DevicesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/devices/new"
          element={
            <ProtectedRoute>
              <AddDevicePage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  )
}

export default App
```

new_string:
```tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { DevicesPage } from './pages/DevicesPage'
import { AddDevicePage } from './pages/AddDevicePage'
import { DeviceDetailPage } from './pages/DeviceDetailPage'
import { ProtectedRoute } from './components/ProtectedRoute'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/devices"
          element={
            <ProtectedRoute>
              <DevicesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/devices/new"
          element={
            <ProtectedRoute>
              <AddDevicePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/devices/:deviceId"
          element={
            <ProtectedRoute>
              <DeviceDetailPage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  )
}

export default App
```

- [ ] **Step 4: Run the full test suite and build**

Run (from `dashboard/`): `npm test`
Expected: every test in the project passes.

Run: `npm run build`
Expected: succeeds with no TypeScript errors.

- [ ] **Step 5: Commit**

```bash
git add dashboard/src/pages/DeviceDetailPage.tsx dashboard/src/pages/DeviceDetailPage.test.tsx dashboard/src/App.tsx
git commit -m "RT: add DeviceDetailPage"
```

---

## Self-Review Notes

- **Spec coverage:** All four routes in `specs/frontend/pages.md`'s table (Task 3: LoginPage, Task 4: DevicesPage, Task 5: AddDevicePage, Task 6: DeviceDetailPage) and every method in `specs/frontend/services.md`'s `ApiClient` (Task 2) are covered. The three open items this plan resolves — React Router version, Tailwind config style, auth persistence — are each closed with a spec edit in the task that made the decision (Task 1 for the first two, Task 3 for the third).
- **Placeholder scan:** No TBD/TODO. The one item that could look like a placeholder — `App.tsx`'s `LoginPagePlaceholder`/`DevicesPagePlaceholder` inline components in Task 1 — is intentional scaffolding explicitly replaced by name in Tasks 3-4, not a gap.
- **Type consistency:** `ApiClient`'s four methods (Task 2) are called with identical signatures everywhere they're consumed: `api.login(username, password)` (Task 3), `api.getDevicesSummary()` (Task 4), `api.registerDevice(deviceId, displayName)` (Task 5), `api.getLatestLocation(deviceId)` (Task 6). `DeviceSummary`, `Location`, `AuthState`, `DeviceRegistrationResult` are imported by name from `./types` (or `../types` from `pages/`/`store/`/`components/`) everywhere, never redefined. Route paths are consistent between where they're linked from (`DevicesPage`'s `Link to="/devices/new"` and `Link to={...}/devices/${device.deviceId}`) and where they're declared (`App.tsx`'s `<Route path="/devices/new">` and `<Route path="/devices/:deviceId">`).
