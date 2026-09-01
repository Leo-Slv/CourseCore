# Implementation Plan — CRUD administrativo de Areas

**Spec:** [Docs/specs/areas/area-crud.md](area-crud.md) (Approved, 2026-09-01)
**Status:** Draft — aguardando aprovação antes de qualquer código.

Este documento é o **HOW**: como implementar o comportamento definido na spec, seguindo a arquitetura descrita em `.claude/claude.md` e os padrões já existentes no módulo `Modules/Access` e em módulos irmãos (`Modules/Users` como referência mais próxima para Create/Update/List). Não deve introduzir nenhuma regra de negócio nova além do que a spec já definiu — se algo aqui parecer uma decisão de negócio, é sinal de que a spec deveria ser atualizada primeiro, não este plano.

## 1. Escopo técnico e ordem de implementação

Segue a direção de dependência do projeto (Presentation → Application → Domain, Infrastructure implementando abstrações). Como Domain e Infrastructure de `Area` já existem e não precisam mudar (ver §2), o trabalho real começa na camada Application.

1. Validation limits (Application)
2. DTOs de Application (Input/Output)
3. Use cases (Application)
4. Constantes de policy e audit log (Auth, AuditLogs)
5. Registro de policy (Auth)
6. Requests/Response/Presenter (Presentation)
7. Controller (Presentation)
8. Registro de DI (Access)
9. Testes (Domain, Application, Integration)
10. Documentação (OpenAPI é automático; Postman, README, diagrama)

## 2. O que NÃO muda

Confirmado por leitura direta do código — nenhum destes arquivos precisa de alteração:

- [Modules/Access/Domain/Entities/Area.cs](../../../Modules/Access/Domain/Entities/Area.cs) — já expõe tudo que os use cases precisam (`Create`, `ChangeName`, `ChangeSlug`, `ChangeDescription`, `ChangeDisplayOrder`, `Activate`, `Deactivate`).
- [Modules/Access/Domain/Repositories/IAreaRepository.cs](../../../Modules/Access/Domain/Repositories/IAreaRepository.cs) — **não será alterado**. Decisão técnica (ver §3.3): o filtro por `active` da listagem é resolvido inteiramente dentro do novo `ListAreasUseCase`, sem tocar a assinatura de `ListAsync`. Isso elimina qualquer risco sobre os dois consumidores existentes em `CourseAccessService` (que continuam chamando `ListAsync(cancellationToken)` exatamente como hoje).
- `EfAreaRepository`, `AreaMapper`, `AreaConfiguration` — sem mudanças.
- Schema/migrations — nenhuma migration nesta etapa (spec §11, Decisão 1).
- `AreasController.cs` existente (`/api/access`) — intocado; CRUD vai para um controller novo.

## 3. Application layer

### 3.1 Validation limits

Novo arquivo: `Modules/Access/Application/Validation/AreaValidationLimits.cs`

Segue o padrão de `CourseValidationLimits`/`UserValidationLimits` (classe estática com constantes `int`):

- `NameMaxLength = 150`
- `SlugMaxLength = 180`
- `DescriptionMaxLength = 500`

Valores extraídos diretamente de `AreaConfiguration.cs` (limites de coluna), não inventados.

### 3.2 DTOs

Pasta `Modules/Access/Application/DTOs/`, seguindo a nomenclatura já usada em `Modules/Users/Application/DTOs` (`CreateUserInput`, `UpdateUserInput`, `UserOutput`, `ListUsersInput`):

- `CreateAreaInput` — `Name`, `Slug`, `Description`, `DisplayOrder`.
- `UpdateAreaInput` — `AreaId`, `Name`, `Slug`, `Description`, `DisplayOrder`, `Active`.
- `ListAreasInput` — `Active` (`bool?`).
- `AreaOutput` — espelha `AreaResponse` da spec (`Id`, `Name`, `Slug`, `Description`, `Active`, `DisplayOrder`, `CreatedAt`, `UpdatedAt`), com um método estático `FromArea(Area area)` (mesmo padrão de `UserOutput.FromUser`).

### 3.3 Use cases

Pasta `Modules/Access/Application/UseCases/`. Cada um recebe suas dependências por construtor, mesmo padrão de `CreateUserUseCase`/`UpdateUserUseCase`.

- **`CreateAreaUseCase`** — depende de `IAreaRepository`, `IUnitOfWork`, `IAuditLogService`. Implementa o fluxo 6.1 da spec: valida formato, valida unicidade de slug via `FindBySlugAsync`, cria via `Area.Create`, persiste, registra `AreaCreated`.
- **`UpdateAreaUseCase`** — mesmas dependências. Implementa o fluxo 6.2: busca por ID (`NotFoundException` se ausente), valida formato, valida conflito de slug ignorando a própria área, aplica só os campos que mudaram (mesmo padrão de flags de mudança de `UpdateUserUseCase`), ativa/desativa conforme o estado recebido, registra `AreaUpdated` sempre e `AreaActivated`/`AreaDeactivated` condicionalmente.
- **`GetAreaByIdUseCase`** — depende só de `IAreaRepository`. Busca por ID, `NotFoundException` se ausente. **Atenção**: este é o primeiro caso de uso "buscar por ID sem lógica adicional" do projeto (spec §2.2) — não há um caso de uso irmão para copiar 1:1; usar `GetCourseDetailsUseCase`/`GetCourseProgressUseCase` só como referência de forma, não de conteúdo (esses dois têm lógica de autorização/derivação que `GetAreaByIdUseCase` não deve ter).
- **`ListAreasUseCase`** — depende só de `IAreaRepository`. Chama `ListAsync(cancellationToken)` (assinatura atual, inalterada) e filtra por `Active` **em memória**, dentro do próprio use case, quando `ListAreasInput.Active` não for nulo. Justificativa da escolha (registrada aqui para não repetir a discussão depois): dataset de baixa cardinalidade (spec já assume isso ao dispensar paginação), e evita qualquer mudança em uma interface consumida por outro serviço do mesmo módulo (`CourseAccessService`).

## 4. Constantes e policy (Auth, AuditLogs)

### 4.1 Audit log actions

Adicionar em [AuditLogActionNames.cs](../../../Modules/AuditLogs/Application/Constants/AuditLogActionNames.cs), mesmo estilo `PascalCase` das existentes:

```text
AreaCreated
AreaUpdated
AreaActivated
AreaDeactivated
```

### 4.2 Policy `ManageAreas`

A permissão `areas.manage` **já existe** (`AuthPermissionNames.ManageAreas`) e já está seedada — não precisa de mudança em `CourseCoreDatabaseSeeder.cs`.

Falta só a policy:

1. Em [AuthPolicyNames.cs](../../../Modules/Auth/Application/Constants/AuthPolicyNames.cs): adicionar `public const string ManageAreas = "ManageAreas";`.
2. Em [AuthDependencyInjection.cs](../../../Modules/Auth/AuthDependencyInjection.cs): adicionar `AddPermissionPolicy(options, AuthPolicyNames.ManageAreas, AuthPermissionNames.ManageAreas);` junto às outras chamadas do mesmo helper. Isso já aplica o fallback para role `Admin` automaticamente (`HasPermissionOrAdmin`), sem código extra.

Spec §11 Decisão 2 confirma: uma única policy para os 4 endpoints, sem policy separada para leitura.

## 5. Presentation layer

Novos arquivos em `Modules/Access/Presentation/`:

- `Requests/CreateAreaRequest.cs`, `Requests/UpdateAreaRequest.cs`, `Requests/ListAreasRequest.cs` (`[FromQuery]`, com `Active` como `bool?`).
- `Responses/AreaResponse.cs`.
- `Presenters/AreaPresenter.cs` — classe estática com `ToInput(CreateAreaRequest)`, `ToInput(Guid areaId, UpdateAreaRequest)`, `ToInput(ListAreasRequest)`, `ToResponse(AreaOutput)`, mesmo padrão de `UserPresenter`. Não reutilizar `AccessPresenter` (esse já existe e é dedicado a grants/checks — misturar responsabilidades ali contrariaria a separação que a spec e o `AreasController` atual já estabelecem).

## 6. Controller

Novo arquivo: `Modules/Access/Presentation/Controllers/AreaManagementController.cs`

- `[ApiController]`, `[Route("api/areas")]`, `[Authorize(Policy = AuthPolicyNames.ManageAreas)]` no nível da classe (mesmo padrão de `UsersController`, já que a policy é igual nos 4 endpoints — spec §11 Decisão 2).
- `POST` → `CreateAsync`, retorna `Created($"/api/areas/{response.Id}", response)` (mesmo padrão de `UsersController.CreateAsync`).
- `PUT {areaId:guid}` → `UpdateAsync`.
- `GET {areaId:guid}` → `GetByIdAsync`.
- `GET` (com `[FromQuery] ListAreasRequest`) → `ListAsync`.
- Cada action documenta `ProducesResponseType` para `200`/`201`/`400`/`401`/`403`/`404` (`Create`/`Update`: também `409`), usando `ApiErrorResponse` para os erros — mesmo padrão de `UsersController`/`CoursesController`.
- Controller não injeta `CourseCoreDbContext` nem repository EF — só os 4 use cases.

Nome escolhido (`AreaManagementController`, não `AreasController`) para não colidir com o controller existente em `/api/access`, que mantém esse nome hoje.

## 7. Registro de DI

Em [AccessDependencyInjection.cs](../../../Modules/Access/AccessDependencyInjection.cs), adicionar ao `AddAccessModule`:

```text
services.AddScoped<CreateAreaUseCase>();
services.AddScoped<UpdateAreaUseCase>();
services.AddScoped<GetAreaByIdUseCase>();
services.AddScoped<ListAreasUseCase>();
```

Nenhuma mudança em `Program.cs` — o módulo já é registrado via `AddAccessModule()`.

## 8. Testes

Seguindo a estrutura de `Tests/CourseCore.Api.Tests/` (Domain / Application / Integration, um arquivo por classe/controller testado).

### 8.1 Domain — lacuna encontrada, não introduzida por esta feature

`Modules/Access/Domain/Entities/Area.cs` **não tem teste de domínio hoje** (`Tests/CourseCore.Api.Tests/Domain/Access/` só tem `RoleAreaAccessTests.cs` e `UserAreaAccessTests.cs`). Como os use cases novos dependem diretamente das regras de `Area` (nome obrigatório, slug obrigatório, `DisplayOrder >= 0`, ativar/desativar), esta etapa deve criar `Tests/CourseCore.Api.Tests/Domain/Access/AreaTests.cs` cobrindo essas regras — é pré-existente, mas fica descoberto se não for testado agora junto com a feature que depende dele.

### 8.2 Application — `Tests/CourseCore.Api.Tests/Application/Access/`

Um arquivo por use case, mesmo padrão de `GrantUserAreaAccessUseCaseTests.cs`:

- `CreateAreaUseCaseTests.cs` — criação válida, nome vazio, nome acima do limite, slug inválido, slug acima do limite, `displayOrder` negativo, slug duplicado (ativo e inativo), audit log registrado, escrita via `IUnitOfWork`.
- `UpdateAreaUseCaseTests.cs` — atualização de todos os campos, ativação, desativação, área inexistente, conflito de slug com outra área, reenvio do mesmo slug (sem conflito consigo mesma), nenhum campo alterado (sem eventos de ativação/desativação), audit logs corretos.
- `GetAreaByIdUseCaseTests.cs` — encontrada (ativa e inativa), não encontrada.
- `ListAreasUseCaseTests.cs` — sem filtro, só ativas, só inativas, lista vazia, ordenação por `DisplayOrder`/`Name`.

### 8.3 Integração — `Tests/CourseCore.Api.Tests/Integration/Access/`

Novo arquivo `AreaManagementIntegrationTests.cs` (não misturar com `AccessIntegrationTests.cs`, que cobre grants/checks de outro controller), usando a mesma factory SQLite in-memory (`CourseCoreApiFactory`) já usada pelos outros testes de integração:

- Criação retorna `201` com `Location` e o recurso.
- Listagem e consulta por ID retornam o recurso criado.
- Atualização altera campos e desativa.
- Slug duplicado retorna `409` (create e update).
- Payload inválido retorna `400`.
- `areaId` inexistente retorna `404` em `GET`/`PUT`.
- `active=xyz` (não booleano) na listagem retorna `400`.
- Sem JWT retorna `401`; com JWT sem `areas.manage`/`Admin` retorna `403`; com `areas.manage` ou `Admin` funciona.
- Endpoints aparecem documentados com segurança Bearer no OpenAPI (mesmo padrão de `OpenApiIntegrationTests.cs`).
- `CourseAccessService`/fluxo de disponibilidade de cursos continua funcionando após desativar uma área usada em teste (regressão do comportamento descrito na Regra de negócio 11 da spec).

## 9. Documentação (após testes verdes)

- `Postman/CourseCore.postman_collection.json` — adicionar `List Areas`, `Get Area By Id`, `Create Area`, `Update Area`, com scripts salvando `areaId`, e um cenário negativo de slug duplicado (mesmo padrão de automação já usado nas outras pastas da collection).
- `Postman/CourseCore.local.postman_environment.json` — variável `areaId` se ainda não existir.
- `Docs/postman.md` — documentar os novos endpoints, policy e status codes.
- `README.md` — atualizar lista de endpoints, se for onde os outros estão listados.
- `Docs/implementation-class-diagram.md` — adicionar `AreaManagementController`, DTOs e use cases novos, para o diagrama não ficar defasado do código (mesmo problema que motivou a spec original).

Nenhuma dessas mudanças de documentação altera comportamento — podem ser feitas depois dos testes passarem, sem bloquear a implementação de código.

## 10. Validação obrigatória antes de considerar a feature pronta

```powershell
dotnet restore
dotnet build
dotnet test
dotnet list package --vulnerable --include-transitive
docker compose config --quiet
```

```powershell
Get-Content Postman/CourseCore.postman_collection.json -Raw | ConvertFrom-Json | Out-Null
Get-Content Postman/CourseCore.local.postman_environment.json -Raw | ConvertFrom-Json | Out-Null
```

```powershell
git status
git status --ignored
git diff --stat
```

Confirmar explicitamente antes do commit:

- nenhuma regra de acesso existente regrediu (`CourseAccessServiceTests.cs` continua verde);
- nenhuma migration foi criada;
- nenhum `database update` ou seed real foi executado durante os testes;
- nenhum secret foi adicionado;
- `.env`, `bin/`, `obj/` e artefatos locais continuam ignorados.

## 11. Fora deste plano

Idêntico ao "Fora de escopo" da spec (§10) — este plano não introduz nada que a spec não previu. Em particular, nenhuma mudança em `IAreaRepository`, nenhuma migration, nenhum tratamento de concorrência além do padrão existente, nenhum bloqueio na desativação.
