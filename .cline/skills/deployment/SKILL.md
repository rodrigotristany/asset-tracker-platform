---
name: deployment
description: Docker Compose orchestration, GitHub Actions CI/CD, and DigitalOcean Droplet production deployment
---

triggers:
  paths:
    - "docker-compose.yml"
    - "Dockerfile"
    - ".github/workflows/**"
    - "deploy/**"
  keywords:
    - "deploy"
    - "docker"
    - "ci/cd"
    - "github actions"
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

Use this skill whenever editing Dockerfiles, docker-compose files, GitHub Actions workflows, production configs, secrets management, or anything related to shipping the application to a production environment.

## Instructions

1. Use Docker Compose for both local development and production. Do not switch to bare-metal process management unless explicitly instructed.
2. Production target for this project is a single DigitalOcean Droplet. Do not implement Terraform, Ansible, or multi-cloud abstractions unless explicitly requested.
3. Never commit secrets, `.env` files, or credentials. Use Docker secrets or environment injection at deploy time.
4. Every deployment change must include a rollback path: describe how to revert to the previous image/version if the deploy fails.
5. Keep Docker images minimal and reproducible. Pin base image digests when practical.
6. Enable Gzip on the FastAPI backend at the reverse proxy or ASGI middleware level.
7. CI/CD should include: lint → test → build → push. If GitHub Actions is used, separate jobs for backend and firmware if both are built in the same workflow.
8. For Phase 2: serve built dashboard static files from FastAPI or a lightweight reverse proxy on the droplet. Avoid extra services unnecessarily.
