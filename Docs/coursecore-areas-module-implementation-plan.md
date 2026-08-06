# CourseCore — Plano de implementação do módulo de Areas

## 1. Objetivo

Implementar o gerenciamento administrativo de áreas da CourseCore API, eliminando a dependência operacional do seed ou de consultas diretas ao banco para obter e manter `areaId`.

Este plano complementa `Docs/implementation-class-diagram.md` e deve preservar a arquitetura modular atual:

```text
Presentation -> Application -> Domain
                         ^
                         |
                  Infrastructure
```

Controllers não devem acessar `CourseCoreDbContext` ou repositories EF concretos. Regras de negócio e validações de existência/unicidade devem ficar em use cases e domínio.

## 2. Diagnóstico atual

O módulo Access já possui:

- entidade `Area`, com criação, alteração de nome/slug/descrição/ordem e ativação/desativação;
- `IAreaRepository` com `FindByIdAsync`, `FindBySlugAsync`, `ListAsync`, `CreateAsync` e `UpdateAsync`;
- `EfAreaRepository`, mapper, persistence model e configuração EF;
- tabela `areas`, índice único de `Slug` e relacionamentos com cursos, usuários e roles;
- permission `areas.manage`;
- grants de acesso por usuário e role;
- seed local com áreas iniciais.

O módulo ainda não possui:

- use cases administrativos de área;
- requests, responses e presenter de área;
- policy dedicada `ManageAreas`;
- controller de recurso em `/api/areas`;
- testes HTTP e unitários dessas operações;
- requests correspondentes na collection Postman.

O atual `AreasController` usa `/api/access` e deve continuar responsável pelos grants e checks existentes. Para evitar mistura de responsabilidades, o CRUD de áreas deve ser exposto por um novo controller em `/api/areas`.

## 3. Escopo funcional

### 3.1 Endpoints mínimos

| Método | Rota | Objetivo | Policy | Resposta principal |
|---|---|---|---|---|
| `GET` | `/api/areas` | Listar áreas ordenadas | `ManageAreas` | `200 AreaResponse[]` |
| `GET` | `/api/areas/{areaId}` | Consultar área por ID | `ManageAreas` | `200 AreaResponse` |
| `POST` | `/api/areas` | Criar área | `ManageAreas` | `201 AreaResponse` |
| `PUT` | `/api/areas/{areaId}` | Atualizar área e seu estado | `ManageAreas` | `200 AreaResponse` |

Não implementar remoção física nesta etapa. Áreas possuem foreign keys com cursos e grants usando `DeleteBehavior.Restrict`; desativação é a operação segura para retirar uma área de uso.

### 3.2 Listagem

Contrato inicial recomendado:

```http
GET /api/areas?active=true
```

Regras:

- `active` é opcional;
- sem filtro, retorna ativas e inativas para administração;
- ordenar por `displayOrder` e depois por `name`;
- retornar uma coleção vazia quando não houver resultados;
- não exigir paginação nesta primeira etapa, pois áreas são dados administrativos de baixa cardinalidade;
- permitir evolução posterior para paginação sem alterar os outros endpoints.

### 3.3 Criação

Request:

```json
{
  "name": "Courses",
  "slug": "courses",
  "description": "Access to course content",
  "displayOrder": 10
}
```

Regras:

- `name` obrigatório, trim e máximo de 150 caracteres;
- `slug` obrigatório, normalizado pelo value object `Slug` e máximo de 180 caracteres;
- `description` opcional no contrato, normalizada para string vazia e máximo de 500 caracteres;
- `displayOrder` maior ou igual a zero;
- nova área nasce ativa;
- slug deve ser único;
- conflito de slug deve retornar `409 Conflict` de forma controlada;
- criação deve ser transacional via `IUnitOfWork`;
- registrar audit log sem informações sensíveis.

### 3.4 Atualização

Request:

```json
{
  "name": "Courses",
  "slug": "courses",
  "description": "Access to course content",
  "displayOrder": 10,
  "active": true
}
```

Regras:

- `areaId` deve ser um GUID válido e não vazio;
- retornar `404 Not Found` quando a área não existir;
- validar os mesmos limites da criação;
- permitir mudança de nome, slug, descrição, ordem e estado ativo;
- impedir conflito com slug de outra área;
- atualizar usando os métodos da entidade `Area`;
- não substituir a entidade por um persistence model no use case;
- registrar audit log com os campos alterados, sem gravar payload completo desnecessariamente.

### 3.5 Consulta por ID

Regras:

- retornar `404 Not Found` quando não encontrada;
- retornar tanto áreas ativas quanto inativas para usuários autorizados;
- não carregar grants ou cursos relacionados nesta etapa;
- manter response simples e estável.

## 4. Contratos HTTP

### 4.1 `CreateAreaRequest`

```csharp
public sealed class CreateAreaRequest
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
}
```

### 4.2 `UpdateAreaRequest`

```csharp
public sealed class UpdateAreaRequest
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public bool Active { get; init; }
}
```

### 4.3 `ListAreasRequest`

```csharp
public sealed class ListAreasRequest
{
    public bool? Active { get; init; }
}
```

### 4.4 `AreaResponse`

```csharp
public sealed class AreaResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool Active { get; init; }
    public int DisplayOrder { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
```

## 5. Application layer

Criar DTOs de entrada/saída coerentes com o padrão dos módulos existentes e os seguintes use cases:

### 5.1 `CreateAreaUseCase`

Dependências:

```text
IAreaRepository
IUnitOfWork
IAuditLogService
```

Fluxo:

1. validar e criar `Slug`;
2. verificar duplicidade com `FindBySlugAsync`;
3. criar a entidade com `Area.Create`;
4. persistir via `CreateAsync` dentro do Unit of Work;
5. registrar `AreaCreated`;
6. retornar output.

### 5.2 `UpdateAreaUseCase`

Dependências:

```text
IAreaRepository
IUnitOfWork
IAuditLogService
```

Fluxo:

1. buscar por ID;
2. retornar `NotFoundException` quando ausente;
3. validar conflito do novo slug ignorando a própria área;
4. aplicar alterações pelos métodos do domínio;
5. ativar ou desativar conforme request;
6. persistir via `UpdateAsync` dentro do Unit of Work;
7. registrar `AreaUpdated`, `AreaActivated` ou `AreaDeactivated` quando aplicável;
8. retornar output.

### 5.3 `GetAreaByIdUseCase`

Dependência:

```text
IAreaRepository
```

Buscar por ID e retornar `NotFoundException` quando ausente.

### 5.4 `ListAreasUseCase`

Dependência:

```text
IAreaRepository
```

O filtro por estado pode ser realizado no repository para evitar carregar linhas descartadas. Evoluir `IAreaRepository.ListAsync` com filtro opcional ou adicionar um método específico, preservando chamadas atuais do `CourseAccessService`.

## 6. Authorization

Adicionar:

```csharp
public const string ManageAreas = "ManageAreas";
```

Registrar a policy com:

```text
permission areas.manage ou role Admin
```

Aplicar `[Authorize(Policy = AuthPolicyNames.ManageAreas)]` no controller inteiro.

Não reutilizar `ManageUserAreaAccess` ou `ManageRoleAreaAccess`: administrar o catálogo de áreas é uma responsabilidade diferente de conceder acesso a usuários ou roles.

## 7. Presentation layer

Arquivos previstos:

```text
Modules/Access/Presentation/Controllers/AreaManagementController.cs
Modules/Access/Presentation/Requests/CreateAreaRequest.cs
Modules/Access/Presentation/Requests/UpdateAreaRequest.cs
Modules/Access/Presentation/Requests/ListAreasRequest.cs
Modules/Access/Presentation/Responses/AreaResponse.cs
Modules/Access/Presentation/Presenters/AreaPresenter.cs
```

O nome `AreaManagementController` evita conflito conceitual com o atual `AreasController`. A rota explícita deve ser `[Route("api/areas")]`; o nome da classe não deve alterar o contrato HTTP.

Status codes principais:

| Operação | Status codes |
|---|---|
| Listar | `200`, `401`, `403`, `500` |
| Consultar | `200`, `400`, `401`, `403`, `404`, `500` |
| Criar | `201`, `400`, `401`, `403`, `409`, `500` |
| Atualizar | `200`, `400`, `401`, `403`, `404`, `409`, `500` |

Usar `ApiErrorResponse` nos metadados OpenAPI, seguindo os controllers atuais.

## 8. Domain e persistência

### 8.1 Reutilização

Reutilizar:

- `Area.Create`;
- `ChangeName`;
- `ChangeSlug`;
- `ChangeDescription`;
- `ChangeDisplayOrder`;
- `Activate` e `Deactivate`;
- `AreaMapper`;
- `EfAreaRepository`;
- índice único existente de slug.

### 8.2 Ajustes esperados

- centralizar limites de Area em uma classe de validation limits do módulo Access;
- validar limites antes de chegar ao banco;
- mapear duplicidade de slug para `ConflictException` em vez de expor `DbUpdateException`;
- adicionar consulta filtrada por `Active` sem quebrar `CourseAccessService`;
- garantir comparação coerente do slug normalizado.

### 8.3 Migration

Não criar migration se o modelo permanecer igual. A tabela, colunas, índice único e relacionamentos necessários já existem.

Migration só será justificável se a implementação decidir alterar schema, índice ou constraints. Essa decisão deve ser diagnosticada e documentada antes de gerar qualquer migration.

## 9. Audit logs

Adicionar action names consistentes:

```text
AreaCreated
AreaUpdated
AreaActivated
AreaDeactivated
```

Metadata recomendada:

```text
areaId
slug
changedFields
```

Não registrar token, credencial, payload completo ou dados pessoais desnecessários.

## 10. Testes

### 10.1 Domain/Application

Cobrir:

- criação válida;
- nome vazio;
- slug inválido;
- `displayOrder` negativo;
- slug duplicado;
- atualização de todos os campos;
- ativação e desativação;
- atualização de área inexistente;
- conflito de slug na atualização;
- listagem sem filtro, somente ativas e somente inativas;
- audit log das ações sensíveis;
- Unit of Work utilizado nas escritas.

### 10.2 Integração HTTP

Cobrir:

- Admin cria área e recebe `201` com `Location`;
- Admin lista e consulta a área criada;
- Admin atualiza e desativa área;
- slug duplicado retorna `409`;
- payload inválido retorna `400`;
- GUID inválido retorna `400` ou não corresponde à rota;
- área inexistente retorna `404`;
- request sem JWT retorna `401`;
- usuário sem `areas.manage` retorna `403`;
- usuário com `areas.manage` recebe `200`/`201`;
- OpenAPI marca os endpoints como Bearer;
- response de erro mantém trace ID e correlation ID.

Os testes de integração devem usar a factory SQLite in-memory existente e não depender de PostgreSQL, seed real ou Docker.

## 11. Postman e documentação

Adicionar à pasta `03 - Access` ou criar subpasta administrativa `03 - Areas`:

```text
List Areas
Get Area By Id
Create Area
Update Area
```

Automação:

- `List Areas` salva o primeiro `id` em `areaId` quando houver item;
- `Create Area` gera slug único e salva `areaId`;
- `Update Area` usa `areaId`;
- adicionar cenário negativo de slug duplicado;
- documentar policy, variáveis e status codes;
- remover a necessidade de consultar o banco para preencher `areaId` quando o usuário possuir `areas.manage`.

Atualizar:

```text
Postman/CourseCore.postman_collection.json
Postman/CourseCore.local.postman_environment.json
Docs/postman.md
README.md
Docs/implementation-class-diagram.md
```

## 12. Sequência recomendada de implementação

1. Diagnosticar limites atuais, índice de slug e impacto no `CourseAccessService`.
2. Criar validation limits, DTOs e use cases.
3. Adicionar `ManageAreas` e registrar a policy.
4. Registrar use cases no `AccessDependencyInjection`.
5. Criar requests, responses e presenter.
6. Criar controller `/api/areas`.
7. Adicionar testes unitários e de integração.
8. Atualizar OpenAPI, Postman, README e diagramas.
9. Executar validações completas.
10. Criar commit somente após todos os checks passarem.

## 13. Validação obrigatória

Executar:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet list package --vulnerable --include-transitive
docker compose config --quiet
```

Validar JSON do Postman:

```powershell
Get-Content Postman/CourseCore.postman_collection.json -Raw | ConvertFrom-Json | Out-Null
Get-Content Postman/CourseCore.local.postman_environment.json -Raw | ConvertFrom-Json | Out-Null
```

Revisar Git:

```powershell
git diff --check
git status
git status --ignored
git diff --stat
```

Confirmar explicitamente:

- nenhuma regra de acesso existente foi regressada;
- grants continuam idempotentes;
- nenhuma migration foi criada sem necessidade;
- nenhum `database update` ou seed real foi executado durante testes;
- nenhum secret foi adicionado;
- `.env`, `bin/`, `obj/` e artifacts continuam ignorados.

## 14. Fora de escopo

Não implementar nesta etapa:

- remoção física de área;
- CRUD de roles ou permissions;
- atribuição de roles a usuários;
- CRUD de cursos, módulos ou aulas além do já existente;
- alteração da regra de acesso a cursos;
- mudanças no seed, exceto documentação estritamente necessária;
- hardening não relacionado ao módulo de Areas.

## 15. Critérios de aceite

- áreas podem ser criadas, consultadas, listadas e atualizadas via API;
- desativação substitui remoção física;
- slug duplicado retorna `409` controlado;
- policy `ManageAreas` exige `areas.manage` ou Admin;
- controller não acessa DbContext ou repository EF concreto;
- escritas usam `IUnitOfWork`;
- ações administrativas geram audit logs;
- Postman captura `areaId` automaticamente;
- OpenAPI documenta autenticação, responses e contratos;
- build e todos os testes passam;
- nenhum secret ou artefato local é versionado.
