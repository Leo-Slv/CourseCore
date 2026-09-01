# CourseCore API

## Visao geral

CourseCore API e um backend modular em ASP.NET Core para uma plataforma de cursos. A API cobre usuarios, roles, permissions, areas, cursos, modulos, aulas, videos, progresso, auditoria e autenticacao JWT.

O projeto ja inclui hardening de configuracao para producao, refresh token persistido com hash e rotacao, permission claims, health checks, observabilidade com correlation id, audit logs de acoes sensiveis, Docker/Docker Compose, CI e testes automatizados.

`GET /api/users` usa paginacao com `page=1`, `pageSize=50` e maximo 100. Cursos disponiveis resolvem acessos de area em lote, e grants repetidos atualizam a concessao existente.

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
Auth__ExposeRefreshTokenInBody
Auth__AllowRefreshTokenInBodyFallback
Auth__RefreshTokenCookie__Name
Auth__RefreshTokenCookie__Path
Auth__RefreshTokenCookie__SameSite
Auth__RefreshTokenCookie__Secure
Auth__RefreshTokenCookie__MaxAgeDays
RateLimiting__Login__PermitLimit
RateLimiting__Login__WindowSeconds
RateLimiting__Refresh__PermitLimit
RateLimiting__Refresh__WindowSeconds
RateLimiting__Logout__PermitLimit
RateLimiting__Logout__WindowSeconds
Progress__LessonCompletionThresholdPercent
Media__Playback__SignedUrlExpirationMinutes
Media__Playback__SigningSecret
Media__Playback__BaseUrl
Media__Playback__AllowedStorageProviders__0
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

O Docker Compose executa o servico one-shot `coursecore-migrations` antes da API. O EF Core consulta `__EFMigrationsHistory`, ignora migrations ja aplicadas e executa somente as pendentes; se houver falha, a API nao inicia. O seed continua desabilitado por padrao (`Seed__Admin__Enabled=false`). Para staging/producao, prefira scripts SQL revisados conforme `Docs/deployment-migrations.md`.

O compose exige ambiente, senha do PostgreSQL, JWT secret, media signing secret e senha de seed explicitamente no `.env`. O override local publica o PostgreSQL; use somente o arquivo base em producao para manter o banco privado. A imagem da API roda com o usuario nao-root nativo do runtime .NET. Veja `Docs/docker.md`.

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

Os testes de seguranca exercitam o pipeline HTTP real para rotacao concorrente e replay de refresh token, logout, revogacao por `TokenVersion`, rate limiting, policies e validacao de requests. Limites menores de rate limiting sao aplicados apenas por configuracao isolada da factory de teste. Nao sao necessarios Docker, PostgreSQL ou servicos externos. A revogacao dinamica de roles nao e exercitada porque a API ainda nao possui operacao publica de remocao/desativacao de role atribuida.

## Postman

O projeto inclui uma collection Postman completa para os 28 endpoints executaveis, com Bearer automatico, refresh por cookie HttpOnly, encadeamento de IDs, cenarios negativos e um environment local somente com placeholders:

```text
Postman/CourseCore.postman_collection.json
Postman/CourseCore.local.postman_environment.json
```

Use `Docs/postman.md` para configurar `baseUrl` e credenciais locais, executar o fluxo numerado no Collection Runner, renovar o access token por cookie e entender quais IDs sao automaticos ou dependem de dados existentes. A API nao expoe atualmente endpoint `me` nem listagem HTTP de audit logs.

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

`/health/live` sempre retorna somente o status agregado. Em `Development`, ready e health incluem detalhes uteis; em producao retornam somente o status e devem permanecer internos, protegidos pela rede ou reverse proxy.

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
- Login, refresh token e logout possuem rate limiting e retornam `429 Too Many Requests` quando o limite configurado e excedido.
- Login de e-mail inexistente executa uma verificacao BCrypt ficticia para reduzir enumeracao por diferenca de tempo.
- Refresh token e persistido somente como hash.
- Refresh token e enviado para clientes web em cookie `HttpOnly`.
- Refresh token possui expiracao, revogacao e rotacao atomica.
- Reutilizacao de refresh token antigo e rejeitada.
- Logout revoga o refresh token da sessao atual.
- Access token continua no body da resposta; refresh token nao e exposto no body em Production.
- JWT inclui roles, permission claims e `token_version`.
- Requests autenticadas validam o usuario atual no banco e rejeitam JWT antigo quando `TokenVersion` diverge ou o usuario esta inativo.
- Atualizacoes criticas de usuario incrementam `TokenVersion` e revogam refresh tokens ativos do usuario afetado.
- Policies usam permissions com fallback para a role `Admin`.
- Fluxos sensiveis usam o usuario autenticado pelo token, nao `userId` enviado pelo cliente.

Policies de acesso sao separadas por operacao:

```text
ManageUserAreaAccess -> users.manage ou Admin
ManageRoleAreaAccess -> roles.manage ou Admin
ManageAreas -> areas.manage ou Admin
CheckOwnCourseAccess -> qualquer usuario autenticado
CheckUserCourseAccess -> users.manage, areas.manage, courses.manage ou Admin
```

`GET /api/access/courses/{courseId}` consulta somente o acesso do usuario autenticado. `GET /api/access/users/{userId}/courses/{courseId}` e a consulta administrativa explicita de um usuario alvo. O antigo `POST /api/access/course/check` permanece deprecated para compatibilidade e continua limitado ao usuario do token. Grants para roles inativas sao rejeitados com conflito conhecido.

CRUD administrativo de areas (`POST/PUT/GET /api/areas`, `GET /api/areas/{areaId}`) fica em um controller separado de `/api/access`, protegido por `ManageAreas`. Nao ha remocao fisica: `active=false` via `PUT` e a forma suportada de retirar uma area de uso, e derruba imediatamente o acesso a cursos vinculados a ela.

## Observabilidade

A API usa o header:

```text
X-Correlation-ID
```

Se o cliente envia um GUID valido, a API preserva o valor. Caso contrario, a API gera um novo correlation id. O response devolve `X-Correlation-ID`, e respostas de erro incluem `traceId` e `correlationId`.

Logs de aplicacao nao devem conter senha, access token, refresh token, hash, JWT secret, connection string ou secrets de ambiente.

## Validacao de requests

Criacao de usuario e seed administrativo usam uma politica central de senha: minimo de 12 caracteres e maximo de 72 bytes UTF-8, alinhado ao limite seguro do BCrypt, sem valores vazios ou senhas comuns basicas. A senha nunca e retornada, auditada ou registrada em logs.

Campos administrativos sao validados antes da persistencia conforme os limites do banco. Create Course aceita ate 50 areas, 50 modulos e 100 aulas por modulo; thumbnails devem ser URLs HTTP(S) absolutas. Videos limitam titulo, descricao, storage key, duracao e tamanho declarado. O corpo HTTP no Kestrel e limitado a 1 MiB.

Erros de validacao conhecidos retornam `400`. Excecoes operacionais inesperadas retornam `500`; fora de Development, mensagens internas nao sao expostas. Respostas de erro preservam `traceId` e `correlationId`.

Cookies de refresh token usam `HttpOnly`, path `/api/auth` e `SameSite=Lax` por padrao. Em Production, o cookie e sempre `Secure`. Esta etapa nao habilita CORS com credentials nem protecao CSRF completa; para deployments cross-site, avalie CORS explicito com credentials, `SameSite=None; Secure` e CSRF token/custom header em etapa propria.

Rate limiting reduz brute force, credential stuffing e abuso operacional, mas nao substitui MFA, CAPTCHA, lockout progressivo ou monitoramento antifraude em uma etapa futura.

Validacao de `token_version` consulta o banco em requests autenticadas. Otimizacoes futuras podem usar cache curto por `userId/tokenVersion`, cache distribuido ou validacao mais seletiva em endpoints sensiveis.

Veja `Docs/observability.md`.

## Progresso de aulas

O cliente registra apenas `watchedSeconds`. A API calcula a conclusao da aula no servidor usando a duracao real do video e o threshold configurado em `Progress:LessonCompletionThresholdPercent`, com default de 90%.

`WatchedSeconds` e monotonico: uma chamada posterior com valor menor nao reduz o progresso ja salvo. Quando existe video com duracao conhecida, o valor salvo tambem e limitado a `Video.DurationSeconds`.

O campo `markAsCompleted` ainda e aceito temporariamente no request para compatibilidade, mas esta deprecated e e ignorado pelo servidor. A aula so e concluida quando o video esta `Ready`, possui `DurationSeconds > 0` e o progresso assistido atinge o percentual minimo configurado. Curso concluido e percentual do curso sao recalculados somente a partir de aulas concluidas por essa regra do servidor.

## Playback de videos

`playbackUrl` em `CreateVideo` continua aceito temporariamente por compatibilidade, mas esta deprecated e e ignorado. O backend nao usa URL arbitraria enviada pelo cliente para marcar video como pronto nem para responder playback.

Videos novos iniciam como `Processing`. Um administrador com `ManageVideos` deve marcar o video como pronto por `POST /api/videos/{id}/ready`, sem enviar URL. O endpoint de playback gera uma URL temporaria assinada no momento da requisicao, com `expiresAt`, usando `Media:Playback:SigningSecret`, `BaseUrl`, `SignedUrlExpirationMinutes` e `AllowedStorageProviders`.

`StorageKey` e uma chave interna de storage, nao uma URL publica. A API rejeita storage keys vazias, URLs completas, path traversal e caracteres inseguros. Integracao com storage/proxy real fica para etapa futura; a URL assinada atual e um contrato seguro para essa integracao.

## Audit logs

Eventos sensiveis auditados:

- `LoginSucceeded`
- `RefreshTokenRotated`
- `RefreshTokenRejected`
- `RefreshTokenReplayDetected`
- `LogoutSucceeded`
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
- `Docs/postman.md`
- `Docs/specs/` — specs e implementation plans por feature (WHAT/WHY na spec, HOW no plan)

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
