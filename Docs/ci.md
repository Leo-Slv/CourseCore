# Continuous Integration

## Objective

The CourseCore CI workflow validates the .NET solution automatically on push and pull request. It focuses on restore, build, tests, and package vulnerability checks.

## When It Runs

The workflow runs on:

```text
push to master or main
pull_request targeting master or main
```

## Workflow

The workflow file is:

```text
.github/workflows/ci.yml
```

It uses:

```text
actions/checkout@v4
actions/setup-dotnet@v4
.NET 10.0.x
ubuntu-latest
```

## Commands

The CI runs:

```bash
dotnet restore
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release --verbosity normal
dotnet list package --vulnerable --include-transitive
```

## Database

The CI does not start PostgreSQL and does not depend on a real database server.

Integration tests use SQLite in-memory through `WebApplicationFactory`, replacing the application `CourseCoreDbContext` during test startup. This keeps the pipeline fast and reproducible.

## Migrations

The CI does not run EF Core migrations and does not execute:

```bash
dotnet ef database update
```

Migration execution must remain a controlled deployment operation, not part of basic validation.

The current CI also does not start PostgreSQL and does not apply generated SQL scripts. A future deployment workflow may generate an idempotent migration script as an artifact:

```bash
dotnet ef migrations script --context CourseCoreDbContext --idempotent --output ./artifacts/migrations/coursecore-migration.sql
```

That future workflow should publish the SQL for review and require manual approval before applying it to staging or production. Production application must not be automatic from the validation CI job.

## Seed

The CI keeps seed disabled with:

```text
Seed__Admin__Enabled=false
```

Tests create only isolated test data in SQLite in-memory. The real application seed is not required and is not executed.

## Local Reproduction

Run the same validation locally with:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet list package --vulnerable --include-transitive
```

## .NET 10

The project targets `net10.0`, so the workflow uses `10.0.x` in `actions/setup-dotnet`. Do not change the project `TargetFramework` to fit the CI.

## Future Steps

Possible future CI improvements:

```text
Docker image build
test coverage reporting
artifact publish
deployment workflow
dependency review
```
