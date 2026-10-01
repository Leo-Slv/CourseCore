# Implementation Plan — Cadastro de visitante de primeira vez

**Spec:** [Docs/specs/visitors/first-time-visitor-registration.md](first-time-visitor-registration.md) (Approved, 2026-10-01)
**Design:** `Docs/design/mockups/Primeira Vez.html` no repo do frontend (`plataforma-vdg`)
**Status:** Implemented (2026-10-01)

Este documento é o **HOW**. Assume as opções **recomendadas** de spec §11 (CAPTCHA Turnstile = sim; só listagem; permissão `visitors.read`; rate limit 10/60 s). Se alguma decisão for diferente, os pontos afetados estão marcados com **[§11.x]**.

## 1. Escopo técnico e ordem de implementação

Módulo novo `Modules/Visitors`, estruturalmente espelhado em `Modules/Testimonials` (o módulo mais recente e mais parecido: entidade simples, submissão + leitura administrativa).

1. Domain — entidade `Visitor`, repositório abstrato
2. Infrastructure — persistence model, configuration, mapper, repositório EF, `DbSet`
3. Migration `AddVisitors`
4. Application — limites de validação, DTOs, use cases
5. Constantes transversais — audit action, permissão, policy, rate limit
6. Presentation — requests, responses, presenter, controller
7. DI do módulo + `Program.cs`
8. Testes (Domain, Application, Integration)
9. Documentação (Postman, backend-pendencies)

## 2. O que NÃO muda

- `Modules/Users`, `Modules/Auth` (exceto constantes de permissão/policy) — visitante não é usuário (spec §4, regra 1).
- `ICaptchaVerificationService` e `TurnstileCaptchaVerificationService` — reaproveitados como estão.
- Validação de configuração de produção — Turnstile já é obrigatório em produção por causa do registro; nada novo a validar.
- Nenhum envio de e-mail; `IEmailSender` não é usado.

## 3. Domain

### 3.1 Entidade

Novo: `Modules/Visitors/Domain/Entities/Visitor.cs` — herda `EntityBase` (dá `Id`, `CreatedAt`, `UpdatedAt`; `CreatedAt` é o `submittedAt` exposto).

```text
Visitor
  Name     : string    (obrigatório, trim, ≥ 2 chars)
  Phone    : string    (só dígitos, 10–11)
  Email    : Email     (value object Shared)
  Address  : string?   (trim, vazio → null)

  static Create(name, phone, email, address)
  static Restore(id, name, phone, email, address, createdAt, updatedAt)
```

Invariantes que pertencem ao domínio ficam aqui (spec regras 2–5, exceto limites de tamanho máximo, que seguem o padrão do projeto de ficar em `*ValidationLimits` na Application + coluna no EF):

- `Name`: `DomainException` se vazio ou com menos de 2 caracteres após `trim`.
- `Phone`: o próprio `Create` normaliza (`Where(char.IsDigit)`) e lança `DomainException` se o resultado não tiver 10 ou 11 dígitos. Normalização no domínio garante que nenhum caminho grave telefone mascarado.
- `Email`: recebido como `Email` (value object já valida/normaliza).
- Sem métodos de mutação — nada na spec altera um cadastro depois de criado.

> Alternativa considerada: value object `PhoneNumber` em `Shared/Domain/ValueObjects`. Descartado agora — `User.Phone` hoje é texto livre e não há segundo consumidor da regra BR de 10–11 dígitos; mover para `Shared` só por reuso hipotético contraria `CLAUDE.md`. Fica como método privado na entidade.

### 3.2 Repositório

Novo: `Modules/Visitors/Domain/Repositories/IVisitorRepository.cs`

```csharp
Task CreateAsync(Visitor visitor, CancellationToken ct = default);
Task<(IReadOnlyCollection<Visitor> Items, int TotalCount)> ListPagedAsync(
    int page, int pageSize, string? search = null, CancellationToken ct = default);
```

Mesma forma de `IUserRepository.ListPagedAsync`.

## 4. Infrastructure

Pasta `Modules/Visitors/Infrastructure/Persistence/`:

- `Models/VisitorPersistenceModel.cs` — `Id`, `Name`, `Phone`, `Email`, `Address`, `CreatedAt`, `UpdatedAt`.
- `Configurations/VisitorConfiguration.cs` — tabela `visitors`; `Name` 200, `Phone` 11, `Email` 320, `Address` 300 (nullable); índice em `CreatedAt` (ordenação da listagem). **Sem índice único** em e-mail/telefone (spec regra 6).
- `Mappers/VisitorMapper.cs` — `ToDomain` (via `Visitor.Restore` + `Email.Create`) / `ToPersistence`.
- `Repositories/EfVisitorRepository.cs`:
  - `ListPagedAsync` ordena por `CreatedAt` desc, depois `Id`. `search`: `Name.Contains(s) || Email.Contains(s.ToLowerInvariant())`, e, se `s` tiver dígitos, `|| Phone.Contains(digitsOf(s))`.

Adicionar em [CourseCoreDbContext.cs](../../../Shared/Infrastructure/Persistence/CourseCoreDbContext.cs):

```csharp
public DbSet<VisitorPersistenceModel> Visitors => Set<VisitorPersistenceModel>();
```

A configuração é aplicada automaticamente por `ApplyConfigurationsFromAssembly`.

## 5. Migration

```bash
dotnet ef migrations add AddVisitors --output-dir Shared/Infrastructure/Persistence/Migrations
```

Revisar o arquivo gerado (só `CreateTable visitors` + índice em `created_at`) antes do commit. Não aplicar automaticamente em startup (já é a regra do projeto, ver [deployment-migrations.md](../../deployment-migrations.md)).

## 6. Application

### 6.1 Limites

`Modules/Visitors/Application/Validation/VisitorValidationLimits.cs`

```text
NameMaxLength    = 200   (igual UserValidationLimits.NameMaxLength)
EmailMaxLength   = 320   (igual UserValidationLimits.EmailMaxLength)
AddressMaxLength = 300
```

### 6.2 DTOs

`Modules/Visitors/Application/DTOs/`:

- `RegisterVisitorInput` — `Name`, `Phone`, `Email`, `Address?`, `CaptchaToken` **[§11.1]**.
- `RegisterVisitorOutput` — `Id`, `SubmittedAt` (sem PII, spec §3.1).
- `ListVisitorsInput` — `Page`, `PageSize`, `Search?`.
- `VisitorOutput` — `Id`, `Name`, `Phone`, `Email`, `Address`, `SubmittedAt`; `FromVisitor(Visitor)`.

### 6.3 Use cases

`Modules/Visitors/Application/UseCases/`:

- **`RegisterVisitorUseCase`** — depende de `ICaptchaVerificationService` **[§11.1]**, `IVisitorRepository`, `IUnitOfWork`, `IAuditLogService`.
  1. Verifica CAPTCHA primeiro (mesmo padrão de `RegisterUseCase`, linha ~71): inválido → `ApplicationValidationException("Captcha is invalid.")`.
  2. Valida tamanhos máximos (`ApplicationValidationException`), depois cria `Email` e `Visitor` — `DomainException` do domínio já vira `400` pelo middleware existente.
  3. Dentro de `_unitOfWork.ExecuteAsync`: `CreateAsync` + `RecordAsync(AuditLogActionNames.VisitorRegistered, "Visitor", visitor.Id, metadata: null)` — **sem PII** (spec regra 9), `userId: null`.
  4. Retorna `RegisterVisitorOutput`.

  Dependência cruzada `Visitors → Auth.Application.Contracts` é aceitável: é um contrato (não implementação), e o projeto já tem precedentes de módulos consumindo contratos/repositórios de outros (ex.: `Testimonials → Users/Courses`). Mover `ICaptchaVerificationService` para `Shared` só faria sentido como refactor separado.

- **`ListVisitorsUseCase`** — depende de `IVisitorRepository`. Valida `Page`/`PageSize` com `PaginationLimits` (cópia da forma de `ListUsersUseCase`), normaliza `Search`, retorna `PagedResult<VisitorOutput>`.

## 7. Constantes transversais

| Arquivo | Adição |
|---|---|
| [AuditLogActionNames.cs](../../../Modules/AuditLogs/Application/Constants/AuditLogActionNames.cs) | `VisitorRegistered` |
| [AuthPermissionNames.cs](../../../Modules/Auth/Application/Constants/AuthPermissionNames.cs) | `ReadVisitors = "visitors.read"` **[§11.3]** |
| [AuthPolicyNames.cs](../../../Modules/Auth/Application/Constants/AuthPolicyNames.cs) | `ReadVisitors` |
| [AuthDependencyInjection.cs](../../../Modules/Auth/AuthDependencyInjection.cs) | `AddPermissionPolicy(options, AuthPolicyNames.ReadVisitors, AuthPermissionNames.ReadVisitors)` |
| [CourseCoreDatabaseSeeder.cs](../../../Shared/Infrastructure/Persistence/Seed/CourseCoreDatabaseSeeder.cs) | `new("visitors.read", "Read visitors", "Read first-time visitor registrations")` — Admin recebe via `EnsureAdminRolePermissions` |
| [RateLimitPolicyNames.cs](../../../Shared/Presentation/RateLimiting/RateLimitPolicyNames.cs) | `VisitorRegistration` |
| [RateLimitOptions.cs](../../../Shared/Presentation/RateLimiting/RateLimitOptions.cs) | `VisitorRegistration { PermitLimit = 10, WindowSeconds = 60 }` **[§11.4]** |
| [RateLimiterExtensions.cs](../../../Shared/Presentation/RateLimiting/RateLimiterExtensions.cs) | `AddPolicy(RateLimitPolicyNames.VisitorRegistration, ...)` |

Se `appsettings*.json` / `.env.example` listarem as chaves de `RateLimiting`, incluir `VisitorRegistration` lá também.

## 8. Presentation

`Modules/Visitors/Presentation/`:

- `Requests/RegisterVisitorRequest.cs` — `Name`, `Phone`, `Email`, `Address?`, `CaptchaToken`.
- `Requests/ListVisitorsRequest.cs` (query) — `Page = PaginationLimits.DefaultPage`, `PageSize = DefaultPageSize`, `Search?` — mesmo formato usado por `ListUsers`.
- `Responses/RegisterVisitorResponse.cs` — `Id`, `SubmittedAt`.
- `Responses/VisitorResponse.cs`, e a resposta paginada no mesmo formato de `UserListResponse`/`VideoPresenter`.
- `Presenters/VisitorPresenter.cs` — `ToInput(...)`, `ToResponse(...)`.
- `Controllers/VisitorsController.cs` — `[ApiController] [Route("api/visitors")] [Authorize]`:

```text
POST /api/visitors   [AllowAnonymous] [EnableRateLimiting(VisitorRegistration)]
                     201 / 400 / 429 / 500
GET  /api/visitors   [Authorize(Policy = ReadVisitors)]
                     200 / 400 / 401 / 403 / 500
```

Nota sobre `Created`: não há `GET /api/visitors/{id}`; usar `StatusCode(201, response)` em vez de inventar uma `Location` inexistente.

Controller fino: só traduz request → input → use case → response. Sem `try/catch` (middleware de exceção existente cuida de `ApplicationValidationException`/`DomainException` → `400`).

## 9. DI

Novo `Modules/Visitors/VisitorsDependencyInjection.cs`:

```csharp
services.AddScoped<IVisitorRepository, EfVisitorRepository>();
services.AddScoped<RegisterVisitorUseCase>();
services.AddScoped<ListVisitorsUseCase>();
```

`Program.cs`: `builder.Services.AddVisitorsModule();` após `AddQuestionsModule()`.

## 10. Testes

Seguindo a organização existente em `Tests/CourseCore.Api.Tests/`:

| Arquivo | Cobre (spec §9) |
|---|---|
| `Domain/Visitors/VisitorTests.cs` | nome < 2 chars rejeitado; telefone mascarado normalizado; 9 e 12 dígitos rejeitados; endereço vazio → null |
| `Application/Visitors/RegisterVisitorUseCaseTests.cs` | captcha inválido → exceção e nada gravado; sucesso grava e audita `VisitorRegistered` **sem PII nos metadados**; duplicata gera segundo registro; limites máximos |
| `Application/Visitors/ListVisitorsUseCaseTests.cs` | validação de página; ordenação desc; passa `search` |
| `Integration/Visitors/VisitorsIntegrationTests.cs` | `POST` anônimo → `201` sem PII no corpo, sem cookie `Set-Cookie`, nenhum `User` criado; `400`s; `GET` anônimo `401`, sem permissão `403`, admin `200`; busca por nome/e-mail/telefone |
| `Integration/Infrastructure/...` (rate limit) | `429` após exceder o limite configurado (seguir o teste de rate limit já existente para registro, se houver) |

Test doubles novos: `FakeVisitorRepository` em `TestDoubles/` (padrão de `FakeTestimonialRepository`). CAPTCHA já tem `FakeCaptchaVerificationService` registrado em `CourseCoreApiFactory`.

## 11. Documentação

- `Postman/CourseCore.postman_collection.json` — pasta "Visitors" com os dois requests.
- `Docs/backend-pendencies/` — nova entrada (ex.: `visitors/first-time-visitor.md`) descrevendo o contrato para o front: endpoint, corpo, resposta, mensagem de erro única e o requisito de token Turnstile **[§11.1]** (o design atual não mostra o widget; front precisa incluí-lo em modo managed/invisible).
- Atualizar o status da spec para `Approved` com data quando as decisões de §11 forem fechadas.

## 12. Riscos

- **Spam de dados pessoais falsos** se §11.1 = não: só o rate limit protege. Mitigável depois sem quebrar contrato se `captchaToken` já for aceito (opcional) desde o início.
- **LGPD**: dados pessoais de não-usuários sem política de retenção. Registrado como fora de escopo na spec; recomendável abrir pendência própria.
- **Totem atrás de NAT**: muitos visitantes num culto podem compartilhar IP. 10/60 s cobre uso humano; ajustável por configuração sem deploy de código.
