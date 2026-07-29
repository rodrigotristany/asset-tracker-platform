---
name: backend-development
description: FastAPI + SQLAlchemy + Pydantic backend development with strict 3-layer architecture
---

triggers:
  paths:
    - "backend/**"
  keywords:
    - "fastapi"
    - "sqlalchemy"
    - "alembic"
    - "pydantic"
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
  - Schema/DTO
  - Repository
  - Service
  - Router
  - Tests

## When to use

Use this skill whenever editing or creating files under `backend/`, including FastAPI routers, Pydantic schemas, SQLAlchemy models, Alembic migrations, services, and repositories.

## Instructions

1. Follow the 3-layer architecture strictly: Routes (`app/routers/`) call Services (`app/services/`) which call Repositories (`app/repositories/`).
2. Define Pydantic DTOs/Schemas for every API contract. Never expose SQLAlchemy models directly in responses.
3. Use FastAPI `Depends` for database session injection. Do not create sessions manually inside route handlers.
4. Table names must be plural `snake_case` (e.g., `locations`). Column names must be `snake_case`.
5. All SQL queries must be parameterized. Never interpolate raw strings into queries.
6. Device API keys are loaded from environment variables via `pydantic-settings` and passed in the `X-API-Key` header.
7. Dashboard authentication uses JWT (`python-jose` + `passlib`) with session storage (cookie or header — TBD exact mechanism; use cookie by default unless instructed otherwise).
8. Return a standardized error envelope on failures when practical:
   {"error": "VALIDATION_ERROR", "message": "...", "details": {}}.
9. Enable CORS for local dashboard development. Serve OpenAPI docs at `/docs` and `/redoc`. Enable Gzip compression.
10. Do NOT create Alembic migrations automatically. Only generate migrations when explicitly requested.
11. After backend changes, run `pytest` to validate behavior before finishing the task.
