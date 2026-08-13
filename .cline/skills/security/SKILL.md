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

1. Device API keys are base64-encoded 32 random bytes; only their SHA-256 hash is ever persisted (`devices.api_key_hash`). Keys are passed in the `X-API-Key` header and validated by `ApiKeyAuthenticationHandler` (decode + re-hash + compare). Never store or log a device's raw API key.
2. Dashboard authentication must use JWT bearer tokens (`Microsoft.AspNetCore.Authentication.JwtBearer`), issued from `POST /api/v1/auth/login`. Passwords are hashed with `BCrypt.Net-Next` (BCrypt) — never a raw/reversible hash. Both the JWT and API-key schemes are referenced via the `AuthSchemes` constants class, never a raw string literal in `[Authorize(AuthenticationSchemes = ...)]`.
3. All incoming data must be validated: `DataAnnotations` attributes on `Application.Dtos` on the backend, TypeScript types on the frontend. No raw pass-through of request bodies.
4. Never log secrets, API keys, passwords, JWT tokens, or session cookies. Redact sensitive fields before logging.
5. `.env` files must never be committed. Enforce `.gitignore` rules. Backend configuration comes from `appsettings.json` (fake local-dev defaults only) plus environment-variable overrides via .NET's double-underscore convention (`ConnectionStrings__Default`, `Jwt__Key`).
6. Production secrets must be injected via Docker secrets or environment variables at runtime. Do not hardcode secrets in source code, Docker images, or committed `appsettings.json` — placeholder values there must be blank/empty so the startup `ValidateOnStart()` checks in `AssetTracker.Infrastructure/DependencyInjection.cs` actually fail fast when an override is missing.
7. JWT implementation must support creation and validation, with issuer, audience, expiry, and signing key all configurable via environment variables. There is no refresh-token flow — re-authenticate via `/api/v1/auth/login` when a token expires.
8. CORS must be enabled for local dashboard development but restricted to known origins in production.
9. When this skill is active, prefer read-only analysis. Only write changes when explicitly instructed to fix a security issue.
