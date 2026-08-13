---
paths:
  - "backend/**"
---

# Backend Guidelines

## Architecture
- Clean Architecture, four projects: `AssetTracker.Domain` → `AssetTracker.Application` → `AssetTracker.Infrastructure` → `AssetTracker.Api`. Dependencies point inward only — `Domain` has zero project references, `Api` may reference `Application` and `Infrastructure`, never the other way around.
- Controllers call `Application` services; services call `Application` repository interfaces; `Infrastructure` implements those interfaces. Controllers must never reference `Infrastructure` types directly.
- Organize controllers by resource (`LocationsController`, `DevicesController`, `AuthController`), all under `api/v1/`.

## Data Access (Hybrid)
- Reads and simple CRUD (e.g. `AdminUser`) go through EF Core (`AssetTrackerDbContext`).
- Location and device write/lookup paths, and retention purge, go through hand-written SQL Server **stored procedures** called via Dapper (`Microsoft.Data.SqlClient` + `Dapper`). Don't add new stored procedures for operations that are simple EF Core CRUD — the split exists to demonstrate real stored-procedure skill on the operations that warrant it, not to route everything through raw SQL.
- Stored procedure files live in `AssetTracker.Infrastructure/Data/StoredProcedures/*.sql`, applied via EF Core migrations (`migrationBuilder.Sql(...)`), and are also the single source of truth for the SQL — don't let the C# and the `.sql` file drift.
- **Migration immutability:** stored-procedure-carrying migrations read their SQL from `Data/StoredProcedures/*.sql` at migration-*execution* time (`File.ReadAllText`), not as an embedded string literal. This keeps the `.sql` files' standalone syntax highlighting/tooling, but it means the file's on-disk content *is* the historical migration content. Once a migration referencing one of these `.sql` files has shipped/been applied anywhere, **never edit that `.sql` file** — two databases that both report the migration as "applied" could otherwise end up with different actual stored procedure bodies, with no record of the drift. To change a stored procedure, add a **new** migration with a new `.sql` file (or a new version of it) instead.
- Stored procedure naming: `usp_<Entity>_<Action>` (e.g. `usp_Location_Insert`). Output/select columns are aliased to PascalCase to match Dapper's default column-to-property mapping.
- Never expose EF Core entities or Dapper row-mapping classes directly in API responses; use `Application.Dtos`.

## Database Conventions
- **Table names:** plural `snake_case` (e.g., `locations`).
- **Column names:** `snake_case`.
- **Migrations:** generated via `dotnet ef migrations add <Name> --project AssetTracker.Infrastructure --startup-project AssetTracker.Api`, applied to a database via `dotnet ef database update --project AssetTracker.Infrastructure --startup-project AssetTracker.Api` (see the root `README.md` for the full local-setup flow, including `docker compose up` and the local `dotnet-ef` tool manifest).
- **Queries:** always parameterized (Dapper's anonymous-object/`DynamicParameters` binding, or EF Core LINQ) — never raw string interpolation into SQL.

## Authentication
- Devices authenticate via `X-API-Key` header: base64-encoded 32 random bytes, validated by decoding + SHA-256 re-hashing + comparing to `devices.api_key_hash`. Never store a device's raw API key — only its hash.
- Dashboard admin authenticates via JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`), issued from `POST /api/v1/auth/login`. Passwords hashed with BCrypt (`BCrypt.Net-Next`), never a raw/reversible hash.
- Two named authentication schemes, both referenced via the `AuthSchemes` constants class — never a raw string literal in an `[Authorize(AuthenticationSchemes = ...)]` attribute.

## Error Responses
- Standardized envelope on every 4xx/5xx:
  ```json
  {
    "error": "VALIDATION_ERROR",
    "message": "Human-readable description",
    "details": {}
  }
  ```
- Exception-driven errors (not-found, conflict, invalid credentials) go through `ErrorHandlingMiddleware`, mapped via custom `Application.Exceptions` types.
- Model validation errors (from `DataAnnotations` attributes) go through `ApiBehaviorOptions.InvalidModelStateResponseFactory` in `Program.cs`, not the middleware — they never reach a controller action to throw.

## Configuration
- `appsettings.json` holds obviously-fake local-dev defaults only — never a real secret.
- Production values come from environment variables (`ConnectionStrings__Default`, `Jwt__Key`, etc.) — the .NET config system's double-underscore convention for nested keys.
- Never commit a real connection string, JWT signing key, or database password.

## Observability
- Unhandled exceptions logged via `ErrorHandlingMiddleware` with the request's `TraceIdentifier`.
- CORS enabled for local dashboard development (`http://localhost:5173`).
- Swagger UI at `/swagger` in the Development environment (Swashbuckle.AspNetCore).
- Gzip response compression enabled (`Microsoft.AspNetCore.ResponseCompression`).
