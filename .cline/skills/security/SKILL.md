---
name: security
description: Enforces authentication, authorization, input validation, and secrets management across all layers
---

triggers:
  paths:
    - "**/auth/**"
    - "**/security/**"
    - "**/*secret*"
    - "**/.env*"
  keywords:
    - "auth"
    - "jwt"
    - "api-key"
    - "password"
    - "secret"
    - "cors"
    - "credentials"

tool_restrictions:
  allowed:
    - read_files
    - search_codebase
  disallowed:
    - write_files
    - run_commands
    - fetch_web_content
  # Lift write restriction only when explicitly fixing a security issue

## When to use

Use this skill whenever touching authentication, authorization, credentials, secrets, CORS, API keys, JWT, input validation, or security-related configuration across firmware, backend, or frontend.

## Instructions

1. Device API keys must be static values loaded from environment variables (no database or config-file storage in Phase 1). They are passed in the `X-API-Key` header.
2. Dashboard authentication must use JWT with session storage (prefer cookie-based session unless explicitly told otherwise). Use `python-jose` + `passlib` on the backend.
3. All incoming data must be validated: Pydantic on the backend, TypeScript types on the frontend. No raw pass-through of request bodies.
4. Never log secrets, API keys, passwords, JWT tokens, or session cookies. Redact sensitive fields before logging.
5. `.env` files must never be committed. Enforce `.gitignore` rules. Use `pydantic-settings` for backend configuration.
6. Production secrets must be injected via Docker secrets or environment variables at runtime. Do not hardcode secrets in source code or Docker images.
7. JWT implementation must support creation, validation, and refresh flows. Token expiry and signing algorithm must be configurable via environment variables.
8. CORS must be enabled for local dashboard development but restricted to known origins in production.
9. When this skill is active, prefer read-only analysis. Only write changes when explicitly instructed to fix a security issue.
