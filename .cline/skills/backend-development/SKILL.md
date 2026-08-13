---
name: backend-development
description: ASP.NET Core + EF Core + Dapper/stored-procedure backend development with strict Clean Architecture layering
---

triggers:
  paths:
    - "backend/**"
  keywords:
    - "aspnet"
    - "asp.net"
    - "ef core"
    - "entity framework"
    - "dapper"
    - "stored procedure"
    - "sql server"
    - "api"
    - "endpoint"

tool_restrictions:
  allowed:
    - read_files
    - write_files
    - run_commands
    - search_codebase
  disallowed:
    - fetch_web_content

workflow_order:
  - Domain entity (if new)
  - Dto
  - Repository interface (Application) + implementation (Infrastructure)
  - Service (Application)
  - Controller (Api)
  - Tests

## When to use

Use this skill whenever editing or creating files under `backend/`, including ASP.NET Core controllers, DTOs, EF Core entities/migrations, Dapper-based repositories, stored procedures, and Application services.

## Instructions

1. Follow Clean Architecture strictly: `Api` (controllers) → `Application` (services, DTOs, repository interfaces) → `Infrastructure` (EF Core, Dapper, security) → `Domain` (entities, zero dependencies). Dependencies point inward only.
2. Define a DTO in `Application/Dtos` for every API contract. Never expose an EF Core entity or a Dapper row-mapping class directly in a controller response.
3. Data access is hybrid: EF Core for reads/simple CRUD; hand-written stored procedures (called via Dapper) for location/device write paths and retention purge. Don't add a stored procedure for something that's simple EF Core CRUD.
4. Table names must be plural `snake_case` (e.g., `locations`). Column names must be `snake_case`. Stored procedures: `usp_<Entity>_<Action>`, with output columns aliased to PascalCase for clean Dapper mapping.
5. All SQL is parameterized — Dapper anonymous-object/`DynamicParameters` binding or EF Core LINQ. Never interpolate raw strings into a query or a stored procedure call.
6. Device API keys: base64-encoded 32 random bytes, only the SHA-256 hash is ever stored, validated in `ApiKeyAuthenticationHandler` via the `X-API-Key` header.
7. Dashboard admin auth: JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`) issued from `POST /api/v1/auth/login`, passwords hashed with `BCrypt.Net-Next`. Both authentication schemes are referenced via the `AuthSchemes` constants class, never a raw string literal.
8. Return the standardized error envelope on failures: `{"error": "VALIDATION_ERROR", "message": "...", "details": {}}`. Exception-driven errors go through `ErrorHandlingMiddleware`; DataAnnotations validation errors go through `ApiBehaviorOptions.InvalidModelStateResponseFactory` in `Program.cs`.
9. Enable CORS for local dashboard development. Serve Swagger UI at `/swagger` (Development only). Enable Gzip response compression.
10. Only generate an EF Core migration when explicitly asked, via `dotnet ef migrations add <Name> --project AssetTracker.Infrastructure --startup-project AssetTracker.Api`. Stored-procedure-carrying migrations need their `Up`/`Down` filled in by hand (see `AssetTracker.Infrastructure/Data/StoredProcedures/*.sql`) — EF's auto-diff only handles schema, not raw SQL objects.
11. After backend changes, run `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj` (from `backend/`) to validate behavior before finishing the task. Integration tests need Docker running (Testcontainers spins up a real SQL Server — never substitute an in-memory/SQLite fake).
