---
name: deployment
description: Docker Compose orchestration, Azure Pipelines CI/CD, and DigitalOcean Droplet production deployment
---

triggers:
  paths:
    - "docker-compose.yml"
    - "Dockerfile"
    - "azure-pipelines.yml"
    - "deploy/**"
  keywords:
    - "deploy"
    - "docker"
    - "ci/cd"
    - "azure pipelines"
    - "droplet"
    - "production"

tool_restrictions:
  allowed:
    - read_files
    - write_files
    - run_commands
    - search_codebase
  disallowed:
    - fetch_web_content

## When to use

Use this skill whenever editing Dockerfiles, docker-compose files, Azure Pipelines definitions, production configs, secrets management, or anything related to shipping the application to a production environment.

## Instructions

1. Use Docker Compose for both local development and production. Do not switch to bare-metal process management unless explicitly instructed.
2. Production target for this project is a single DigitalOcean Droplet. Do not implement Terraform, Ansible, or multi-cloud abstractions unless explicitly requested.
3. Never commit secrets, `.env` files, or credentials. Use Docker secrets or environment injection at deploy time. Committed `appsettings.json` placeholders for `Jwt:Key` / `ConnectionStrings:Default` must be blank, not filled-in-looking fake values, so the app's `ValidateOnStart()` fail-fast checks actually catch a deploy that forgot to set the real env vars.
4. Every deployment change must include a rollback path: describe how to revert to the previous image/version if the deploy fails.
5. Keep Docker images minimal and reproducible. Pin base image digests when practical. The backend `Dockerfile` restores/builds against the SDK version pinned in `backend/global.json` — keep that pin (and its `rollForward` policy) consistent with what's actually available in the `mcr.microsoft.com/dotnet/sdk` image tags used by the build stage.
6. Enable Gzip response compression on the ASP.NET Core backend (`Microsoft.AspNetCore.ResponseCompression`, wired via `app.UseResponseCompression()` in `Program.cs`) at the app level; a reverse proxy in front of it is optional.
7. CI/CD for the backend runs on Azure Pipelines (`azure-pipelines.yml`): install the pinned .NET SDK → `dotnet restore` → `dotnet build` → `dotnet test` → publish test results. Keep backend and firmware CI as separate jobs/stages if both are ever built in the same pipeline.
8. For Phase 2: serve built dashboard static files from ASP.NET Core (`UseStaticFiles`/`UseSpaStaticFiles`) or a lightweight reverse proxy on the droplet. Avoid extra services unnecessarily.
