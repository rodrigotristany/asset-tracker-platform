# Backend Rewrite: C#/.NET — Design

**Date:** 2026-08-12
**Author:** Rodrigo Tristany
**Status:** Approved

## 1. Context

The original spec (`specs/spec.md` §6) defined the backend as Python/FastAPI/SQLAlchemy/PostgreSQL. No backend code has been written yet — only specs and agent rule files reference that stack. This is therefore a specification change, not a code migration: there is nothing to port, only documents to rewrite before implementation begins.

**Motivation:** This project doubles as interview preparation for a role requiring strong .NET/C#, SQL Server (MSSQL 2008+) including stored procedures and database architecture, OOP principles, and Azure DevOps exposure. Design choices below are deliberately weighted toward demonstrating those specific skills, not just toward the leanest way to prove the GPS-to-dashboard pipeline.

**Scope:** Backend only. Firmware (C++/ESP-IDF) is unchanged. Frontend (React/TypeScript) required updates for the new endpoints below — see `docs/superpowers/specs/2026-08-14-frontend-backend-alignment-design.md`. Existing endpoints keep camelCase field names and unchanged shapes; firmware integrations require no changes.

## 2. Technology Stack

| Component | Choice | Rationale |
|---|---|---|
| Runtime | .NET (latest LTS at implementation time) | Standard enterprise choice, long support window |
| API framework | ASP.NET Core, Controller-based MVC | Matches the traditional enterprise pattern implied by an MSSQL 2008+ shop, rather than modern Minimal APIs |
| Database | SQL Server | Direct JD requirement ("MSSQL 2008 or higher") |
| Data access | Hybrid: EF Core for reads/simple CRUD; hand-written stored procedures (via ADO.NET/Dapper) for the write paths | Demonstrates both ORM fluency and raw SQL/stored-procedure skill — the two explicitly named JD skills |
| Testing | xUnit + Testcontainers (SQL Server Linux image) | Mirrors the existing `.clinerules/testing.md` policy against in-memory DB fakes, now for MSSQL |
| CI | Azure Pipelines (`azure-pipelines.yml`) | Direct JD requirement; tangible artifact in the repo |
| Deployment | Docker Compose (API + SQL Server container), same DigitalOcean droplet target as before | Continuity with existing infra plan |

**Known risk:** SQL Server needs materially more RAM (~2GB minimum) than the PostgreSQL it replaces. The spec's droplet may be memory-constrained (1–2GB). Not blocking for local development or portfolio purposes; revisit before any real production deploy.

## 3. Database Architecture

The schema expands from a single `locations` table to three normalized tables, giving real surface area for foreign keys, indexing, and stored procedures.

```sql
devices
  id              INT IDENTITY PK
  device_id       VARCHAR(64) UNIQUE NOT NULL   -- business key, e.g. "goat-001"
  display_name    VARCHAR(128) NULL
  api_key_hash    VARBINARY(64) NOT NULL        -- hashed, never stored plaintext
  is_active       BIT NOT NULL DEFAULT 1
  created_at      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()

admin_users
  id              INT IDENTITY PK
  username        VARCHAR(64) UNIQUE NOT NULL
  password_hash   VARBINARY(64) NOT NULL
  created_at      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()

locations
  id              BIGINT IDENTITY PK
  device_fk       INT NOT NULL FOREIGN KEY REFERENCES devices(id)
  timestamp       DATETIMEOFFSET NOT NULL
  latitude        FLOAT NOT NULL
  longitude       FLOAT NOT NULL
  altitude        FLOAT NULL
  speed           FLOAT NULL
  satellites      TINYINT NULL
  hdop            FLOAT NULL
  battery_voltage FLOAT NULL
  is_stale        BIT NOT NULL DEFAULT 0
  created_at      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()

CREATE INDEX idx_locations_device_timestamp ON locations (device_fk, timestamp DESC);
```

**Retention:** 30-day rolling window, enforced by `usp_Retention_PurgeOldLocations` run on a schedule. Scheduling mechanism is an open TBD — see §11.

**Stored procedures** (naming convention: `usp_<Entity>_<Action>`):

| Procedure | Purpose |
|---|---|
| `usp_Location_Insert` | Single location write |
| `usp_Location_BatchInsert` | Batch location write |
| `usp_Location_GetLatestByDevice` | Single-device read — latest location for one device |
| `usp_Location_GetLatestForAllDevices` | Dashboard list read — latest location per device, across all devices with recorded history |
| `usp_Device_Register` | Create a device, return its (hashed-and-stored) API key once |
| `usp_Device_GetByApiKeyHash` | Device auth lookup |
| `usp_Retention_PurgeOldLocations` | 30-day cleanup |

EF Core handles simple reads (e.g. device list) and migrations for schema management; the procedures above are checked into source control as `.sql` scripts and applied via migration.

## 4. Backend Project Architecture

Clean/Onion Architecture as separate class library projects. This is more structure than a 4-endpoint API strictly requires, but multi-project layering is standard practice in the kind of enterprise .NET shop this design targets, and is itself commonly discussed in interviews — so it's treated as in-scope for the portfolio goal, not just pipeline-proving.

```
backend/
├── AssetTracker.Domain/          # Entities (Location, Device, AdminUser) — zero external dependencies
├── AssetTracker.Application/     # Services, DTOs, interfaces (IRepository, IService)
├── AssetTracker.Infrastructure/  # EF Core DbContext, repositories, stored-procedure calls, migrations
├── AssetTracker.Api/             # Controllers, Program.cs, middleware
├── AssetTracker.Tests/           # Unit + integration tests (xUnit)
└── AssetTracker.sln
```

This preserves the existing repo-wide rule (`.clinerules/architecture.md`) of strict layering — Routes → Services → Repositories — just expressed as physical project boundaries instead of folders within one project. Pydantic-DTOs-never-import-models becomes: `Domain` entities never referenced directly by `Api` controllers; DTOs live in `Application` and controllers map to/from them.

## 5. Authentication

Same two-actor split as the original spec, translated to C#:

- **Devices:** `X-API-Key` header, validated against `devices.api_key_hash` via `usp_Device_GetByApiKeyHash`.
- **Dashboard admin:** username/password (BCrypt-hashed) checked against `admin_users`; `POST /api/v1/auth/login` issues a JWT via `Microsoft.AspNetCore.Authentication.JwtBearer`, returned in the JSON response body as `{ token: string }`. The frontend stores the token and sends it as `Authorization: Bearer <token>` on subsequent requests — no cookie is used.

Not using full ASP.NET Core Identity — it's heavier than this scope needs and would obscure the hand-built OOP/security fundamentals that are more relevant to demonstrate here.

## 6. API Endpoints

| Method | Route | Auth | Notes |
|---|---|---|---|
| `POST` | `/api/v1/locations` | API key | Unchanged from original spec |
| `POST` | `/api/v1/locations/batch` | API key | Unchanged |
| `GET` | `/api/v1/locations/{deviceId}` | JWT | Unchanged |
| `GET` | `/api/v1/health` | None | Unchanged |
| `POST` | `/api/v1/devices` | JWT (admin) | **New** — device registration, required now that `locations.device_fk` has a real FK constraint |
| `GET` | `/api/v1/devices` | JWT (admin) | **New** — latest location per device, backed by `usp_Location_GetLatestForAllDevices`; powers the devices list dashboard page |
| `POST` | `/api/v1/auth/login` | None | **New** — explicit login endpoint issuing the JWT (implicit/unspecified in the original spec) |

Request/response JSON bodies (camelCase field names, shapes) for the pre-existing endpoints (`/api/v1/locations`, `/api/v1/locations/batch`, `/api/v1/health`) are unchanged from the original spec — firmware integrations require no changes there. The endpoints marked **New** above required matching frontend spec updates — see `docs/superpowers/specs/2026-08-14-frontend-backend-alignment-design.md`.

## 7. Testing Strategy

- **Unit tests:** xUnit, covering services and business logic (e.g. retry/`is_stale` handling equivalents, validation rules).
- **Integration tests:** xUnit + Testcontainers spinning up a real SQL Server container (no in-memory/SQLite fakes), covering controllers end-to-end including stored procedure calls.
- **Coverage target:** carried over from `.clinerules/testing.md` — 80% of `backend/` code, every endpoint tested for happy path, validation errors, and auth failures.

## 8. CI/CD

`azure-pipelines.yml` at the repo root: restore → build → `dotnet test` (against a Testcontainers-provisioned SQL Server) → optionally build/push a Docker image on merge to `main`. Scope kept to build+test for now, consistent with the original spec's "Phase 1: manual deploy" stance; this is chiefly meant as a demonstrable Azure DevOps artifact.

## 9. Documentation Impact

The following existing files describe the Python/FastAPI stack and need to be rewritten for C#/.NET as part of implementation:

- `specs/spec.md` §6 (Backend Specification)
- `specs/backend/api.md`, `specs/backend/models.md`, `specs/backend/schemas.md`
- `specs/diagrams.md` (backend layer diagram, data flow references to FastAPI/PostgreSQL)
- `.clinerules/backend.md` (rewritten for ASP.NET Core / EF Core / stored procedures conventions)
- `.clinerules/architecture.md` (backend layer separation section)
- `.clinerules/coding.md` (add a C#-specific naming convention section — PascalCase for types/members, camelCase for locals/parameters — since the current blanket "functions/variables: snake_case" rule is Python/C++-flavored and incorrect for C#)
- `.cline/skills/backend-development/SKILL.md` (rewritten triggers, keywords, and workflow order for ASP.NET Core/EF Core/stored procedures)

## 10. Non-Goals

Unchanged from the original spec: analytics/AI, geofencing, mobile apps, complex dashboards, performance optimization beyond proving the pipeline. Device management stays minimal — registration only, no update/deactivate UI in this phase.

## 11. Open TBDs

| Topic | Status |
|---|---|
| Retention purge scheduling mechanism | TBD — SQL Server Agent job vs. external scheduler (e.g. hosted background service); decide at implementation time |
| Production droplet memory sizing for SQL Server | TBD — current droplet plan may be undersized; revisit before production deploy |
| .NET version pin | TBD — use latest LTS available when implementation starts |
