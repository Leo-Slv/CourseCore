# Production Configuration

This document describes the runtime configuration required to run CourseCore API in staging or production.

## Configuration Sources

`appsettings.json` contains safe defaults only. It must not contain real production secrets, real production connection strings, passwords, tokens, or seed credentials.

`appsettings.Development.json` may contain local development values for a developer workstation.

`.env` is a local convenience file created for storing current development values outside the repository. It is ignored by Git and is not loaded automatically by the application.

`.env.example` is versioned and contains placeholder values that can be copied into a terminal, Docker, CI/CD, or a secret manager.

ASP.NET Core reads operating system environment variables by default. Use double underscores for nested settings, for example `Jwt__SecretKey`.

## Required Production Variables

Production must provide these settings through environment variables or a secret manager:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__CourseCoreDatabase=Host=your-host;Port=5432;Database=coursecore;Username=coursecore_user;Password=CHANGE_ME
Jwt__SecretKey=CHANGE_ME_USE_A_LONG_RANDOM_SECRET
Jwt__Issuer=CourseCore
Jwt__Audience=CourseCore
Jwt__AccessTokenExpirationMinutes=60
Jwt__RefreshTokenExpirationDays=7
Cors__AllowedOrigins__0=https://your-frontend-domain.com
Seed__Admin__Enabled=false
```

`Jwt__SecretKey` must be a long random value with at least 32 characters. Do not reuse the development secret.

`ConnectionStrings__CourseCoreDatabase` must point to the production PostgreSQL instance. Do not commit real credentials.

`Cors__AllowedOrigins` must list only trusted frontend origins. Do not use wildcard origins in production.

## Production Startup Validation

When `ASPNETCORE_ENVIRONMENT` is `Production`, the API validates critical configuration during startup and fails fast if any required value is missing or still uses a placeholder.

The validated settings are:

```text
ConnectionStrings:CourseCoreDatabase
Jwt:SecretKey
Jwt:Issuer
Jwt:Audience
Cors:AllowedOrigins
```

The validator does not log secrets or print the full connection string.

## CORS

The API uses the `CourseCoreCorsPolicy` CORS policy.

Development uses configured local origins from `appsettings.Development.json`, with a fallback to:

```text
http://localhost:3000
https://localhost:3000
```

Production requires configured origins and does not use `AllowAnyOrigin`. Credentials are not enabled because the API uses Bearer tokens.

## HTTPS and HSTS

`UseHttpsRedirection` is enabled.

`UseHsts` is enabled outside Development.

When deploying behind a reverse proxy or load balancer, configure forwarded headers carefully with trusted proxies or networks. Forwarded headers are not enabled in this step because they require deploy-specific infrastructure details.

## Scalar and OpenAPI

Scalar and `/openapi/v1.json` are exposed only in Development. They are not exposed in Production by the default pipeline.

## Seed

The database seed remains Development-only and opt-in. It runs only when the app is in Development and `Seed:Admin:Enabled` is `true`.

Do not enable seed in Production unless a controlled operational procedure explicitly requires it.

## Migrations

The application does not apply migrations during startup.

Apply migrations outside the app startup, through a controlled local command, deployment job, or reviewed SQL script. Do not run `dotnet ef database update` automatically in production startup.

For staging and production, generate an idempotent SQL script and review it before applying:

```bash
./scripts/generate-migration-script.sh
```

or on Windows:

```powershell
./scripts/generate-migration-script.ps1
```

The generated artifact belongs under `artifacts/migrations/` and must not contain secrets. Keep database credentials in the deployment environment, CI/CD secret store, or database administration tool.

Before production migration execution:

```text
backup or snapshot the database
review potentially destructive SQL
plan rollback
apply with an authorized database user
validate /health/ready after execution
```

Seed must remain disabled in Production unless a controlled operational procedure explicitly enables it for a one-time action.

## Docker

The repository includes a Dockerfile and docker-compose file for local or staging-like execution. They use environment variables and placeholders only.

Do not copy `.env` into images or commit real Docker secrets. For local container instructions, see `Docs/docker.md`.

## Health Checks

The API exposes:

```text
/health/live
/health/ready
/health
```

`/health/live` validates the process. `/health/ready` validates database connectivity. The health response does not include secrets.
