# asset-tracker-platform
Generic asset tracker platform. Portfolio project to showcases embedded C++, networking, backend APIs, and system architecture.

You will find all the sensitive information in specs documents.

## Getting Started (Backend)

The backend (`backend/`) is an ASP.NET Core / EF Core + Dapper / SQL Server API. All commands below are run from `backend/`.

### 1. Restore local .NET tools

A local tool manifest (`backend/.config/dotnet-tools.json`) pins `dotnet-ef` so migration commands are reproducible on a fresh clone without a global tool install:

```bash
dotnet tool restore
```

### 2. Run the full stack with Docker Compose

```bash
export MSSQL_SA_PASSWORD='<a-strong-password>'
export JWT_KEY='<a-random-secret-at-least-32-bytes>'
docker compose up --build
```

This starts SQL Server and the API container with real connection-string/JWT values injected via environment variables (see `backend/docker-compose.yml`). It does **not** create the database schema automatically — run the migration step below once the `db` service is healthy:

```bash
dotnet tool run dotnet-ef database update \
  --project AssetTracker.Infrastructure \
  --startup-project AssetTracker.Api \
  --connection "Server=localhost,1433;Database=AssetTrackerDb;User Id=sa;Password=$MSSQL_SA_PASSWORD;TrustServerCertificate=True;"
```

(The migrations also seed a development `admin` user — see `specs/backend/models.md` for the seeded credentials, and rotate them before any non-local deploy.)

### 3. Applying migrations without Docker

If you're running SQL Server yourself (not via Compose), point at it with the connection string configured in `appsettings.Development.json`/environment variables and run:

```bash
dotnet ef database update --project AssetTracker.Infrastructure --startup-project AssetTracker.Api
```

(`dotnet ef migrations add <Name>` follows the same `--project`/`--startup-project` pattern — see `.clinerules/backend.md` for migration conventions, including the rule that `.sql` files backing stored-procedure migrations must never be edited after they've shipped.)

### 4. Run tests

```bash
dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj
```

Integration tests require Docker (Testcontainers spins up a real SQL Server instance).