---
name: frontend-development
description: React 18+ TypeScript dashboard with Vite, Tailwind, TanStack Query, Zustand, and strict folder structure
---

triggers:
  paths:
    - "dashboard/**"
  keywords:
    - "react"
    - "vite"
    - "tailwind"
    - "typescript"
    - "dashboard"
    - "frontend"

tool_restrictions:
  allowed:
    - read_files
    - write_files
    - run_commands
    - search_codebase
    - fetch_web_content
  disallowed: []

## When to use

Use this skill whenever editing or creating files under `dashboard/`, including React components, pages, hooks, services, routing, Vite config, Tailwind config, and TypeScript types.

## Instructions

1. Enforce the folder structure:
   ```
   src/
   ├── components/  # Reusable UI components
   ├── pages/       # Route-level views
   ├── hooks/       # Custom hooks
   └── services/    # API client layer
   ```
2. Use **TanStack Query** for all server state (fetching, caching, background refetching, loading/error states).
3. Use **Zustand** only for local UI state (UI toggles, modals, filters). Do not use Zustand for server-derived data.
4. API client types in `src/services/` must match backend Pydantic models exactly. Prefer generating types from the OpenAPI spec (`/docs`) when the backend is running; otherwise mirror the spec schemas manually until automation is in place.
5. Use TypeScript strict mode. No `any` unless explicitly justified.
6. Use Tailwind CSS for styling. Utility classes first; avoid inline styles.
7. Dashboard is read-only for Phase 1. Authenticated with JWT session. No edit/create/delete flows.
8. Do **not** add maps or map markers. Do **not** add WebSocket real-time updates; polling via TanStack Query is acceptable.
9. Routing: use React Router v6. Keep routes aligned with Phase 1/Phase 2 feature scope.
10. After frontend changes, run `npm run lint` / `npm run build` (or equivalent) to verify TypeScript compilation.
