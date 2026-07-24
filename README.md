# CourseCore API

## Visao geral

CourseCore API e um backend modular em ASP.NET Core para uma plataforma de cursos. A API cobre usuarios, roles, permissions, areas, cursos, modulos, aulas, videos, progresso, auditoria e autenticacao JWT.

O projeto ja inclui hardening de configuracao para producao, refresh token persistido com hash e rotacao, permission claims, health checks, observabilidade com correlation id, audit logs de acoes sensiveis, Docker/Docker Compose, CI e testes automatizados.

## Stack

- .NET 10
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- JWT Bearer
- BCrypt
- Scalar/OpenAPI
- Docker e Docker Compose
- GitHub Actions
- xUnit
- SQLite in-memory nos testes de integracao HTTP

## Arquitetura

O projeto segue Clean Architecture / DDD modular, organizado por modulos de negocio:

```text
Modules/
  Auth/
  Users/
  Access/
  Courses/
  Media/
  Progress/
  AuditLogs/
Shared/
```

As entidades de dominio ficam separadas dos `PersistenceModels`. O `CourseCoreDbContext` mapeia apenas modelos de persistencia. Controllers chamam use cases, use cases dependem de interfaces, e repositories EF convertem `PersistenceModel <-> Domain` por mappers.

Para detalhes, veja `Docs/coursecore-implementation-plan-modular.md` e `Docs/implementation-class-diagram.md`.

## Pre-requisitos

- Git
- .NET SDK 10
- PostgreSQL, para rodar localmente sem Docker
- Docker Desktop, para rodar com Docker Compose
- Ferramenta `dotnet-ef`, para comandos de migrations

Comandos uteis:

```powershell
dotnet --version
dotnet tool list
docker --version
docker compose version
```

Se o `dotnet-ef` nao estiver disponivel, instale ou restaure conforme a sua configuracao local:

```powershell
dotnet tool install --global dotnet-ef
```

## Configuracao de ambiente

Arquivos e fontes de configuracao:

- `appsettings.json`: defaults seguros. Nao deve conter secrets reais.
- `appsettings.Development.json`: valores locais de desenvolvimento.
- `.env`: arquivo local opcional, ignorado pelo Git. Nao e carregado automaticamente pela API.
- `.env.example`: arquivo versionado com placeholders para copiar e adaptar fora do repositorio.
- Variaveis de ambiente: fonte recomendada para secrets em homologacao/producao.

Principais variaveis da API:

```text
ASPNETCORE_ENVIRONMENT
ConnectionStrings__CourseCoreDatabase
Jwt__SecretKey
Jwt__Issuer
Jwt__Audience
Jwt__AccessTokenExpirationMinutes
Jwt__RefreshTokenExpirationDays
Cors__AllowedOrigins__0
Seed__Admin__Enabled
Seed__Admin__Name
Seed__Admin__Email
Seed__Admin__Password
Seed__Admin__ResetPassword
```

Variaveis usadas pelo Docker Compose:

```text
POSTGRES_DB
POSTGRES_USER
POSTGRES_PASSWORD
POSTGRES_PORT
COURSECORE_API_HTTP_PORT
```

Em producao, configure secrets e connection strings por variaveis protegidas ou secret manager. Nao coloque senha, JWT secret, token ou connection string real em arquivos versionados.

## Rodando localmente sem Docker

1. Restaure e compile:

```powershell
dotnet restore
dotnet build
```

2. Garanta que o PostgreSQL local esta rodando.

3. Configure a connection string local por variavel de ambiente ou pelo `appsettings.Development.json`.

Exemplo via PowerShell, usando placeholders:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:ConnectionStrings__CourseCoreDatabase="Host=127.0.0.1;Port=5432;Database=coursecore;Username=SEU_USUARIO;Password=SUA_SENHA;Timeout=15;Command Timeout=60"
```

4. Aplique migrations manualmente:

```powershell
dotnet ef database update --context CourseCoreDbContext
```

5. Rode a API:

```powershell
dotnet run
```

Em Development, a API expõe Scalar/OpenAPI. A porta exata depende do profile local em `Properties/launchSettings.json` ou da variavel `ASPNETCORE_URLS`.

## Rodando com Docker

Valide a configuracao do compose:

```powershell
docker compose config
```

Suba API e PostgreSQL:

```powershell
docker compose up --build
```

Logs:

```powershell
docker compose logs -f coursecore-api
docker compose logs -f coursecore-postgres
```

Parar containers:

```powershell
docker compose down
```

Aviso: `docker compose down -v` remove o volume local do PostgreSQL e apaga os dados locais do banco.

O Docker Compose nao aplica migrations automaticamente e nao roda seed por padrao. `Seed__Admin__Enabled=false` e o default. Enquanto o schema nao tiver sido aplicado, `/health/live` pode responder, mas `/health/ready` pode falhar.

## Banco de dados e migrations

Migrations nao rodam automaticamente no startup da API. Aplique localmente apenas quando apropriado:

```powershell
dotnet ef migrations list --context CourseCoreDbContext
dotnet ef database update --context CourseCoreDbContext
```

Para homologacao e producao, gere um SQL idempotente e revise antes de aplicar:

```powershell
dotnet ef migrations script --context CourseCoreDbContext --idempotent --output ./artifacts/migrations/coursecore-migration.sql
```

Ou use os scripts do projeto:

```powershell
./scripts/generate-migration-script.ps1
```

```bash
./scripts/generate-migration-script.sh
```

O SQL gerado fica em `artifacts/migrations/`, que e ignorado pelo Git. Veja `Docs/deployment-migrations.md`.

## Seed admin local

O seed admin e idempotente, opt-in e roda somente em `Development`. Ele nao aplica migrations; o schema precisa estar atualizado antes.

Exemplo seguro com placeholders:

```powershell
$env:Seed__Admin__Enabled="true"
$env:Seed__Admin__Name="CourseCore Admin"
$env:Seed__Admin__Email="admin@coursecore.local"
$env:Seed__Admin__Password="CHANGE_ME_LOCAL_ONLY"
$env:Seed__Admin__ResetPassword="false"
dotnet run
```

Para redefinir a senha local de forma intencional:

```powershell
$env:Seed__Admin__ResetPassword="true"
```

Veja `Docs/database-seeding.md`.

## Executando testes

```powershell
dotnet test
dotnet test --configuration Release
```

Os testes de integracao HTTP usam SQLite in-memory via `WebApplicationFactory`. Eles nao dependem de PostgreSQL real, nao executam migrations e nao rodam seed real.

## Health checks

Endpoints:

```text
GET /health/live
GET /health/ready
GET /health
```

- `/health/live`: valida o processo da API e nao depende do banco.
- `/health/ready`: valida conectividade/preparo do banco.
- `/health`: agrega os checks configurados.

## Scalar/OpenAPI

Em `Development`:

```text
GET /openapi/v1.json
GET /scalar
```

O Bearer JWT aparece por endpoint protegido. Endpoints publicos como login e refresh token permanecem sem requisito Bearer na documentacao.

Scalar/OpenAPI nao sao expostos por padrao em `Production`.

## Autenticacao e autorizacao

- Login emite JWT.
- Refresh token e persistido somente como hash.
- Refresh token possui expiracao, revogacao e rotacao.
- Reutilizacao de refresh token antigo e rejeitada.
- JWT inclui roles e permission claims.
- Policies usam permissions com fallback para a role `Admin`.
- Fluxos sensiveis usam o usuario autenticado pelo token, nao `userId` enviado pelo cliente.

## Observabilidade

A API usa o header:

```text
X-Correlation-ID
```

Se o cliente envia um GUID valido, a API preserva o valor. Caso contrario, a API gera um novo correlation id. O response devolve `X-Correlation-ID`, e respostas de erro incluem `traceId` e `correlationId`.

Logs de aplicacao nao devem conter senha, access token, refresh token, hash, JWT secret, connection string ou secrets de ambiente.

Veja `Docs/observability.md`.

## Audit logs

Eventos sensiveis auditados:

- `LoginSucceeded`
- `RefreshTokenRotated`
- `RefreshTokenRejected`
- `UserCreated`
- `UserUpdated`
- `UserAreaAccessGranted`
- `RoleAreaAccessGranted`
- `CourseCreated`
- `CourseUpdated`
- `CoursePublished`
- `VideoCreated`

Audit logs registram metadados seguros e correlation id quando disponivel. Nao sao auditados senha, JWT, refresh token, hash do refresh token, storage key, playback URL ou secrets.

## CI/CD

O workflow fica em `.github/workflows/ci.yml` e roda em `push` e `pull_request` para `master` e `main`.

Ele executa:

```text
dotnet restore
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release --verbosity normal
dotnet list package --vulnerable --include-transitive
```

O CI nao usa PostgreSQL real, nao aplica migrations e nao roda seed real. Veja `Docs/ci.md`.

## Estrutura do projeto

```text
CourseCore/
  Modules/
    Auth/
    Users/
    Access/
    Courses/
    Media/
    Progress/
    AuditLogs/
  Shared/
  Docs/
  Tests/
  scripts/
```

## Documentacao adicional

- `Docs/coursecore-implementation-plan-modular.md`
- `Docs/coursecore-extra-implementation-plan.md`
- `Docs/coursecore-post-readiness-extra-plan.md`
- `Docs/implementation-class-diagram.md`
- `Docs/production-configuration.md`
- `Docs/docker.md`
- `Docs/database-seeding.md`
- `Docs/deployment-migrations.md`
- `Docs/observability.md`
- `Docs/ci.md`

## Cuidados de producao

- Configurar secrets via variaveis protegidas ou secret manager.
- Configurar connection string real fora do repositorio.
- Configurar CORS restrito.
- Configurar HTTPS/HSTS e reverse proxy conforme a infraestrutura.
- Avaliar `ForwardedHeaders` com proxies confiaveis.
- Aplicar migrations fora do startup, com SQL revisado.
- Manter seed desabilitado.
- Nao expor Scalar/OpenAPI em `Production`.
- Validar `/health/live`, `/health/ready` e `/health` apos deploy.
- Configurar logs, metricas e monitoramento externo conforme a operacao evoluir.
