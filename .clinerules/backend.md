---
paths:
  - "backend/**"
---

# Backend Guidelines

## API Design
- Organize routers by **resource** (`locations`, `devices`, `auth`), all under `/api/v1/`.
- Use **dependency injection** for database sessions (FastAPI `Depends`).
- Never expose SQLAlchemy models directly in API responses; use separate Pydantic DTOs.

## Database Conventions
- **Table names:** Plural `snake_case` (e.g., `locations`).
- **Column names:** `snake_case`.
- **Migrations:** `alembic/versions/001_<brief_description>.py`
- **Queries:** All queries must be parameterized. No raw string interpolation in SQL.

## Authentication
- Device API keys stored in **environment variables** (no database/config file in Phase 1).
- Devices authenticate via `X-API-Key` header.
- JWT implementation details TBD; recommended stack: `python-jose` + `passlib`.
- Dashboard uses JWT with session storage (cookie or header-based; TBD).

## Error Responses
- Standardized format:
  ```json
  {
    "error": "VALIDATION_ERROR",
    "message": "Human-readable description",
    "details": {}
  }
  ```
- Use pragmatic FastAPI defaults; do not over-customize status codes.
- Validation errors may use Pydantic's default format or the standardized wrapper above.

## Configuration
- All secrets and environment-specific values loaded from `.env` files.
- Use `pydantic-settings` for configuration management.
- Never commit `.env` to version control.

## Service Layer
- Routers must call service functions; services contain business logic.
- Repositories/data access must be isolated in a dedicated module.
- Routers must not import from `app/repositories/` directly; they import from `app/services`.

## Observability
- All log entries must include `request_id` (from middleware).
- All log entries involving a device must include `device_id`.
- JSON structured logs in production; human-readable in development.
- Enable CORS for local dashboard development.
- Serve OpenAPI docs at `/docs` (Swagger) and `/redoc`.
- Enable Gzip compression for responses.
