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
Auth__ExposeRefreshTokenInBody=false
Auth__AllowRefreshTokenInBodyFallback=false
Auth__RefreshTokenCookie__Name=coursecore_refresh_token
Auth__RefreshTokenCookie__Path=/api/auth
Auth__RefreshTokenCookie__SameSite=Lax
Auth__RefreshTokenCookie__Secure=true
Auth__RefreshTokenCookie__MaxAgeDays=7
RateLimiting__Login__PermitLimit=5
RateLimiting__Login__WindowSeconds=60
RateLimiting__Login__QueueLimit=0
RateLimiting__Refresh__PermitLimit=20
RateLimiting__Refresh__WindowSeconds=60
RateLimiting__Refresh__QueueLimit=0
RateLimiting__Logout__PermitLimit=30
RateLimiting__Logout__WindowSeconds=60
RateLimiting__Logout__QueueLimit=0
Progress__LessonCompletionThresholdPercent=90
Media__Playback__SignedUrlExpirationMinutes=10
Media__Playback__SigningSecret=CHANGE_ME_USE_A_SEPARATE_MEDIA_SIGNING_SECRET
Media__Playback__BaseUrl=https://media.your-domain.com
Media__Playback__AllowedStorageProviders__0=Local
Cors__AllowedOrigins__0=https://your-frontend-domain.com
Seed__Admin__Enabled=false
```

`Jwt__SecretKey` must be a long random value with at least 32 characters. Do not reuse the development secret.

`ConnectionStrings__CourseCoreDatabase` must point to the production PostgreSQL instance. Do not commit real credentials.

`Cors__AllowedOrigins` must list only trusted frontend origins. Do not use wildcard origins in production.

`Auth__ExposeRefreshTokenInBody` must remain `false` in Production. The API also suppresses refresh tokens in response bodies when running in Production even if this setting is accidentally enabled.

`Auth__AllowRefreshTokenInBodyFallback` should remain `false` for web/PWA production deployments. The preferred flow stores the refresh token in the `coursecore_refresh_token` cookie.

`Auth__RefreshTokenCookie__Secure` must be `true` in Production. The application forces refresh token cookies to `Secure` in Production. `SameSite=Lax` is the default same-site posture; use `SameSite=None` only with `Secure=true` when a cross-site frontend/API deployment explicitly requires it.

`RateLimiting__Login`, `RateLimiting__Refresh`, and `RateLimiting__Logout` configure fixed-window limits for the public authentication endpoints. Defaults are 5 login attempts, 20 refresh attempts, and 30 logout attempts per minute per remote IP. Exceeded limits return `429 Too Many Requests` and may include `Retry-After`.

`Progress__LessonCompletionThresholdPercent` configures the minimum watched percentage required for server-side lesson completion. The default is 90 and valid values are 1 through 100. The API ignores client-controlled `markAsCompleted`, keeps watched seconds monotonic, and clamps watched seconds to the video duration when the lesson has a known video duration.

`Media__Playback__SigningSecret` signs temporary playback URLs. Use a long random secret that is separate from `Jwt__SecretKey`. Do not commit the real value. `Media__Playback__SignedUrlExpirationMinutes` defaults to 10 and must be between 1 and 60. `Media__Playback__BaseUrl` is the media/proxy base URL used in generated signed playback URLs. `Media__Playback__AllowedStorageProviders` restricts which stored video providers can generate playback URLs.

## Production Startup Validation

When `ASPNETCORE_ENVIRONMENT` is `Production`, the API validates critical configuration during startup and fails fast if any required value is missing or still uses a placeholder.

The validated settings are:

```text
ConnectionStrings:CourseCoreDatabase
Jwt:SecretKey
Jwt:Issuer
Jwt:Audience
Media:Playback:SigningSecret
Media:Playback:BaseUrl
Media:Playback:SignedUrlExpirationMinutes
Media:Playback:AllowedStorageProviders
Cors:AllowedOrigins
Auth:RefreshTokenCookie:Secure
```

Production also rejects equal JWT/media signing secrets and rejects a non-secure refresh-token cookie. `SameSite=None` is valid only with `Secure=true`.

The validator does not log secrets or print the full connection string.

## CORS

The API uses the `CourseCoreCorsPolicy` CORS policy.

Development uses configured local origins from `appsettings.Development.json`, with a fallback to:

```text
http://localhost:3000
https://localhost:3000
```

Production requires configured origins and does not use `AllowAnyOrigin`. Credentials are not enabled because the API uses Bearer tokens.

Refresh token cookies are scoped to `/api/auth`. If a cross-site frontend needs browser credentials for auth endpoints, configure CORS carefully with explicit origins and credentials in a future step. Do not combine credentials with wildcard origins.

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

Security Hardening 05 adds `TokenVersion` to `users`. Deploy the reviewed migration before running an API version that validates the `token_version` JWT claim against the database.

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

Authenticated requests now query the current user to reject inactive users and stale JWTs. Monitor database latency and consider a short user token-version cache in a future performance hardening step if needed.

## Docker

The base Compose file requires environment, PostgreSQL password, JWT secret, media signing secret, and seed password explicitly and does not publish PostgreSQL. The automatically loaded override publishes PostgreSQL for local tooling only; do not apply that override in production. The API image runs as the built-in non-root .NET user.

Do not copy `.env` into images or commit real Docker secrets. For local container instructions, see `Docs/docker.md`.

## Health Checks

The API exposes:

```text
/health/live
/health/ready
/health
```

`/health/live` is the only endpoint intended for public exposure and always returns aggregate status only. `/health/ready` validates database connectivity and `/health` aggregates checks; both must be restricted by the private network or reverse proxy. They return component names and durations only in Development, and aggregate status only in Production.
