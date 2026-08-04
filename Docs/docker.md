# Docker

## Objective

This setup runs CourseCore API and PostgreSQL with reproducible local containers. It does not embed secrets, does not apply migrations automatically, and keeps seed disabled by default. It is not a production deployment definition.

## Files

`Dockerfile` builds and publishes the API with a multi-stage .NET 10 build. The final image uses the ASP.NET runtime image and listens on HTTP port `8080`.

`docker-compose.yml` defines the internal API/database network without publishing PostgreSQL. `docker-compose.override.yml` is loaded automatically for local commands and publishes PostgreSQL for tools such as DBeaver or pgAdmin.

```text
coursecore-api
coursecore-postgres
```

`.dockerignore` keeps local artifacts, Git metadata, logs, and `.env` files out of the Docker build context.

## Prerequisites

Install Docker with Compose support.

Validate the local installation with:

```bash
docker --version
docker compose version
```

## Local Environment

Copy `.env.example` to `.env` and replace placeholder values locally. Do not commit `.env`.

The compose file reads `.env` automatically through Docker Compose interpolation. The API itself does not load `.env`; it receives configuration through container environment variables.

Required local values (Compose fails before startup when one is absent):

```text
POSTGRES_DB=coursecore
POSTGRES_USER=coursecore_user
POSTGRES_PASSWORD=<local-random-password>
COURSECORE_API_HTTP_PORT=8080
POSTGRES_PORT=5432
ASPNETCORE_ENVIRONMENT=Development
Jwt__SecretKey=<random-secret-at-least-32-characters>
Media__Playback__SigningSecret=<different-random-secret-at-least-32-characters>
Seed__Admin__Password=NOT_USED_WHILE_SEED_IS_DISABLED
```

Generate separate random JWT and media secrets, for example with `openssl rand -base64 48`. Never commit `.env`. The seed password variable is required to prevent an implicit known default, but is not used while `Seed__Admin__Enabled=false`.

## Start

```bash
docker compose up --build
```

Run in the background:

```bash
docker compose up --build -d
```

These local commands load `docker-compose.override.yml` and publish PostgreSQL on `POSTGRES_PORT`. To validate or run the safer base definition without a host database port, use `docker compose -f docker-compose.yml config` or `docker compose -f docker-compose.yml up --build`.

## Stop

```bash
docker compose down
```

Remove containers and the PostgreSQL volume:

```bash
docker compose down -v
```

## Logs

```bash
docker compose logs -f coursecore-api
docker compose logs -f coursecore-postgres
```

## Health Checks

With the default port:

```text
http://localhost:8080/health/live
http://localhost:8080/health/ready
http://localhost:8080/health
```

`/health/live` is public and returns only aggregate status. `/health/ready` checks database connectivity and `/health` aggregates checks. Development returns component details for the latter two; non-Development environments return only aggregate status. In production, expose only `/health/live` publicly and restrict `/health/ready` and `/health` at the private network or reverse proxy.

## API and Scalar

The API is available at:

```text
http://localhost:8080
```

When `ASPNETCORE_ENVIRONMENT=Development`, Scalar is available at:

```text
http://localhost:8080/scalar
```

## Migrations

Migrations are not applied automatically during container startup.

`docker-compose.yml` starts PostgreSQL and the API only. It does not run `dotnet ef database update`, does not run `Database.Migrate()`, and does not execute SQL migration scripts automatically.

The API can start before the database has the expected schema. In that case, `/health/live` can still report the process as healthy, while `/health/ready` or endpoints that query the database can fail until migrations are applied through a controlled process.

For local development, apply migrations manually through a controlled command when needed. From the host machine with the .NET SDK installed and the PostgreSQL container running:

```bash
dotnet ef database update --context CourseCoreDbContext
```

For staging or production, prefer a reviewed SQL script generated from EF Core migrations:

```bash
./scripts/generate-migration-script.sh
```

or on Windows:

```powershell
./scripts/generate-migration-script.ps1
```

The generated SQL goes to `artifacts/migrations/`, which is ignored by Git. Review and apply it with credentials supplied outside the repository. Do not apply migrations as part of normal API startup.

## Seed

Seed is disabled by default in Docker:

```text
Seed__Admin__Enabled=false
```

For local development only, set these values in `.env`:

```text
Seed__Admin__Enabled=true
Seed__Admin__Password=CHANGE_ME_LOCAL_ONLY
```

Keep seed disabled for staging and production unless a controlled operational procedure explicitly requires it.

## Production Care

Do not version real passwords, JWT secrets, tokens, or production connection strings. Provide production values through the hosting platform, CI/CD secrets, or a secret manager.

Production deployments should use `docker-compose.yml` without the local override (or a platform-specific deployment), keep PostgreSQL private, terminate HTTPS at a trusted reverse proxy, and define migration, secret management, logging, backup, and persistence policies. The runtime image uses the built-in non-root .NET user (`APP_UID`).
