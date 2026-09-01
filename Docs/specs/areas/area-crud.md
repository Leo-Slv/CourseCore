# Spec — CRUD administrativo de Areas

**Status:** Approved
**Aprovada em:** 2026-09-01

## 1. Objetivo

Permitir que administradores criem, consultem, listem e atualizem `Area` via API HTTP, eliminando a dependência de seed ou de acesso direto ao banco para obter/manter `areaId`. Areas são o catálogo de segmentos de acesso (ex.: "Courses", "Media") usados por `UserAreaAccess`, `RoleAreaAccess` e pela associação `Course <-> Area`.

Esta spec complementa e reconcilia `Docs/implementation-class-diagram.md` e `Docs/coursecore-areas-module-implementation-plan.md` com o estado real do código, verificado nesta análise. Ela é a fonte de verdade para o **comportamento** da feature — divergências entre esta spec e os dois documentos anteriores devem ser resolvidas a favor desta spec. Ela não é um guia de implementação: mecanismo interno (nomes de classes, exceptions, assinaturas de método) é decisão da implementação, orientada por `.claude/claude.md` e pelos padrões já existentes no código.

## 2. Contexto

### 2.1 O que já existe (verificado no código)

- Entidade de domínio `Area` ([Modules/Access/Domain/Entities/Area.cs](../../../Modules/Access/Domain/Entities/Area.cs)) com criação, alteração de nome/slug/descrição/ordem e ativação/desativação. Validações de formato de campo (obrigatoriedade, trim, `DisplayOrder >= 0`) já vivem no domínio.
- `IAreaRepository` ([Modules/Access/Domain/Repositories/IAreaRepository.cs](../../../Modules/Access/Domain/Repositories/IAreaRepository.cs)) já expõe busca por ID, busca por slug, listagem, criação e atualização, além dos métodos de grants (não afetados por esta spec).
- `EfAreaRepository`, mapper e configuração EF já implementados — ver [Modules/Access/Infrastructure/Persistence](../../../Modules/Access/Infrastructure/Persistence).
- Tabela `areas` já existe com índice único **global** em `Slug` (não filtrado por `Active`), `Name` (max 150), `Slug` (max 180), `Description` (max 500, coluna não aceita `NULL` — só string vazia).
- Permissão `areas.manage` já está definida ([Modules/Auth/Application/Constants/AuthPermissionNames.cs](../../../Modules/Auth/Application/Constants/AuthPermissionNames.cs)) e já seedada em [CourseCoreDatabaseSeeder.cs](../../../Shared/Infrastructure/Persistence/Seed/CourseCoreDatabaseSeeder.cs). Ela já é usada hoje como parte da policy composta `CheckUserCourseAccess`. **Não existe ainda** uma policy dedicada `ManageAreas`.
- `CourseAccessService` ([Modules/Access/Application/Services/CourseAccessService.cs](../../../Modules/Access/Application/Services/CourseAccessService.cs)) consome a listagem de áreas para calcular quais cursos um usuário pode acessar, filtrando por `Active` em memória. Qualquer mudança no contrato de listagem de Areas precisa preservar esse consumidor sem quebrar compilação nem comportamento.
- O `AreasController` atual ([Modules/Access/Presentation/Controllers/AreasController.cs](../../../Modules/Access/Presentation/Controllers/AreasController.cs)) usa rota `api/access` e é responsável apenas por **grants** e **checks de acesso a curso**. Ele não deve ganhar os endpoints de CRUD — outra responsabilidade, outro controller.
- O projeto já usa o constraint de rota `{param:guid}` (ex.: `AreasController.cs`, rota `courses/{courseId:guid}`), e não há nenhuma customização de `ApiBehaviorOptions`/`SuppressModelStateInvalidFilter` em [Program.cs](../../../Program.cs) — o comportamento padrão do `[ApiController]` (400 automático em falha de model binding) está ativo em todo o projeto.

### 2.2 O que não existe e esta spec cobre

- Use cases administrativos de Area (criar, atualizar, buscar por ID, listar).
- Requests, response e presenter de Area na camada de apresentação.
- Policy dedicada `ManageAreas`.
- Um novo controller em `/api/areas`.
- Mapeamento de conflito de slug para `409` (hoje uma violação do índice único não é tratada explicitamente por nenhum use case do projeto).
- Uma operação de busca por ID de Area sem lógica adicional de autorização/derivação — não existe precedente direto disso no projeto hoje: `UsersController` não tem `GetByIdAsync`, e os únicos casos de uso `Get...` existentes (`GetCourseDetailsUseCase`, `GetCourseProgressUseCase`) embutem regras de acesso ou cálculo, não são uma busca simples. Esta feature introduz esse padrão pela primeira vez.

### 2.3 Convenções de implementação

Não repetidas aqui. Regras de arquitetura, separação de camadas, tratamento de erro, autorização e padrão de transação já estão definidas em `.claude/claude.md` e devem ser seguidas como estão — junto com os padrões observáveis em módulos existentes (`Modules/Users` é a referência mais próxima para Create/Update/List; não há referência direta para a busca por ID, ver §2.2). Esta spec define **o que** o sistema deve fazer; **como** isso é construído em C#/EF Core é responsabilidade da implementação.

## 3. Comportamento esperado

A API deve expor 4 operações sobre o catálogo de Areas, todas atrás de autenticação e da nova policy `ManageAreas`:

| Operação | Método | Rota | Sucesso |
|---|---|---|---|
| Criar | `POST` | `/api/areas` | `201 Created` + `Location` + `AreaResponse` |
| Atualizar | `PUT` | `/api/areas/{areaId}` | `200 OK` + `AreaResponse` |
| Consultar por ID | `GET` | `/api/areas/{areaId}` | `200 OK` + `AreaResponse` |
| Listar | `GET` | `/api/areas?active={bool?}` | `200 OK` + `AreaResponse[]` |

`AreaResponse`:

```json
{
  "id": "guid",
  "name": "string",
  "slug": "string",
  "description": "string",
  "active": true,
  "displayOrder": 0,
  "createdAt": "2026-01-01T00:00:00Z",
  "updatedAt": "2026-01-01T00:00:00Z"
}
```

## 4. Regras de negócio

1. `Name`: obrigatório, sem espaços nas pontas, máximo 150 caracteres.
2. `Slug`: obrigatório, deve seguir o formato `minusculas-com-hifen` (letras minúsculas, números e hífen simples entre segmentos — sem espaço, acento ou maiúscula), máximo 180 caracteres. A API não converte texto livre em slug automaticamente — o cliente deve enviar um slug já no formato aceito.
3. `Slug` é único entre todas as Areas, incluindo inativas. Decisão confirmada (ver §11, Decisão 1): a unicidade global é mantida como está, sem índice parcial e sem migration — reutilizar o slug de uma Area desativada exige reativá-la ou renomeá-la primeiro.
4. `Description`: opcional no contrato HTTP; ausência é normalizada para string vazia; máximo 500 caracteres.
5. `DisplayOrder`: inteiro `>= 0`; não há exigência de unicidade — Areas com a mesma ordem são desempatadas por `Name` em qualquer listagem.
6. Toda Area nasce ativa. Não existe campo para definir o estado inicial na criação.
7. O estado ativo/inativo só é alterado via atualização completa do recurso (`PUT`). Não há remoção física nesta etapa — Areas possuem vínculos restritos (curso, grants) que impedem exclusão sem antes desfazer essas relações; desativação é a operação segura disponível.
8. Cada operação de escrita (criação ou atualização) deve ser atômica: se qualquer validação ou verificação de conflito falhar, nenhuma alteração parcial pode ficar persistida.
9. Toda escrita (criação, atualização, mudança de estado ativo/inativo) gera um registro de auditoria. Hoje nenhum campo de `Area` é sensível ou pessoal, então esta feature não introduz necessidade de mascarar dado algum — segue a prática geral de auditoria já usada no projeto para outras entidades.
10. A policy `ManageAreas` autoriza usuários com a permissão `areas.manage` OU role `Admin` — mesmo modelo de autorização já usado por todas as outras policies administrativas do projeto (`ManageUsers`, `ManageCourses`, `ManageVideos`).
11. Desativar uma Area (`active: false`) tem efeito imediato sobre o acesso de usuários aos cursos vinculados a essa área: cursos de áreas inativas deixam de ser contabilizados como acessíveis. Este é um comportamento **já existente** em `CourseAccessService` (não introduzido por esta feature), mas passa a ser acionável via API assim que o `PUT` existir. Decisão confirmada (ver §11, Decisão 4): o efeito é imediato e silencioso — a API não bloqueia nem avisa sobre cursos/grants vinculados no momento da desativação.

## 5. Pré-condições

- Requisição autenticada com JWT válido, respeitando a policy `ManageAreas`.
- Para `PUT`/`GET /{areaId}`: `areaId` deve ser um `Guid` sintaticamente válido. O projeto já usa o constraint de rota `{param:guid}` em outros endpoints (ex.: rotas de curso em `AreasController.cs`), então o comportamento esperado para um `areaId` malformado é a rota não corresponder a nenhum endpoint (`404`) — mas isso é uma inferência a partir da convenção existente, não há hoje um teste no projeto comprovando esse comportamento para nenhum endpoint. Recomenda-se um teste de integração explícito para fechar essa lacuna.
- Para `Create`: nenhuma Area com o mesmo `Slug` normalizado pode existir (ativa ou inativa).
- Para `Update`: a Area referenciada por `areaId` deve existir; se o `Slug` do payload mudar, nenhuma **outra** Area pode possuir esse novo `Slug`.

## 6. Fluxo principal

### 6.1 Criar

1. Os dados recebidos são validados quanto a formato e tamanho.
2. O sistema garante que o `Slug` informado não está em uso por nenhuma outra Area (ativa ou inativa).
3. A Area é criada já no estado ativo.
4. A alteração é registrada como um evento de auditoria (`AreaCreated`).
5. O cliente recebe `201 Created`, com o endereço do recurso criado no header `Location` e o recurso no corpo.

### 6.2 Atualizar

1. A Area referenciada por `areaId` deve existir; caso contrário, a operação falha (ver §7).
2. Os dados recebidos são validados quanto a formato e tamanho, da mesma forma que na criação.
3. Se o `Slug` estiver mudando, o sistema garante que o novo valor não está em uso por **nenhuma outra** Area.
4. Nome, slug, descrição, ordem de exibição e estado ativo/inativo são atualizados para os valores recebidos.
5. A alteração é registrada como evento de auditoria (`AreaUpdated`); se o estado ativo/inativo mudou, um evento adicional específico é registrado (`AreaActivated` ou `AreaDeactivated`).
6. O cliente recebe `200 OK` com o recurso atualizado.

### 6.3 Consultar por ID

1. A Area referenciada por `areaId` deve existir; caso contrário, a operação falha (ver §7).
2. Areas ativas e inativas são retornadas da mesma forma — não há filtro de status nesta operação.
3. O cliente recebe `200 OK` com o recurso.

### 6.4 Listar

1. O cliente pode opcionalmente pedir só Areas ativas ou só inativas via um filtro de status; sem filtro, todas são retornadas.
2. Ausência de resultados retorna uma lista vazia, nunca um erro.
3. O resultado é ordenado por ordem de exibição e, em caso de empate, por nome.
4. O cliente recebe `200 OK` com a lista.

## 7. Cenários de erro

| Cenário | Operação(ões) | HTTP |
|---|---|---|
| Sem JWT / token inválido | todas | `401` |
| JWT válido sem `areas.manage` e sem role `Admin` | todas | `403` |
| `Name` vazio, só espaços, ou acima do limite | Create, Update | `400` |
| `Description` acima do limite | Create, Update | `400` |
| `Slug` vazio, fora do formato aceito, ou acima do limite | Create, Update | `400` |
| `DisplayOrder` negativo | Create, Update | `400` |
| Valor de `active` na query string não é um booleano válido | Listar | `400` (comportamento padrão do model binding do ASP.NET, não customizado neste projeto — ver §2.1) |
| `Slug` já usado por outra Area (ativa ou inativa) | Create, Update | `409` |
| `areaId` não corresponde a nenhuma Area existente | Update, Consultar | `404` |
| `areaId` sintaticamente inválido (não é um GUID) | Update, Consultar | `404` (ver ressalva em §5 sobre este comportamento não ter teste de precedente direto) |
| Corpo da requisição malformado (JSON inválido / tipo de campo incorreto) | Create, Update | `400` |
| Erro inesperado não coberto pelos cenários acima | todas | `500` |

## 8. Casos de borda

- **`PUT` sem alterar nenhum campo**: deve retornar `200` com o estado atual inalterado; o evento de auditoria de ativação/desativação só é gerado se o estado realmente mudou, mas o evento de atualização geral ainda é registrado.
- **`Slug` reenviado igual ao atual no `PUT`**: não deve ser tratado como conflito consigo mesmo.
- **Criar Area com `Slug` idêntico a uma Area *inativa***: deve ser rejeitado com `409`, dado que a unicidade é global (Regra de negócio 3, §11 Decisão 1).
- **Listagem sem nenhuma Area cadastrada**: retorna `200` com lista vazia, nunca erro.
- **`DisplayOrder` duplicado entre duas Areas**: permitido; não é um erro nem precisa ser bloqueado.
- **Dois requests concorrentes criando a mesma `Slug`**: decisão confirmada (§11, Decisão 3) — sem tratamento especial para essa corrida, consistente com o resto do projeto; na pior hipótese uma das duas requisições pode falhar com `500` em vez de `409` sob concorrência real.
- **Atualizar a mesma Area duas vezes seguidas com o mesmo payload**: idempotente em termos de estado final, mas um novo evento de auditoria de atualização é registrado a cada chamada — sem deduplicação.
- **Desativar uma Area que tem cursos ou grants vinculados**: permitido sem bloqueio, decisão confirmada (Regra de negócio 11, §11 Decisão 4).

## 9. Critérios de aceite

- [ ] Admin autenticado com `areas.manage` ou role `Admin` consegue criar, listar, consultar por ID e atualizar Areas via `/api/areas`.
- [ ] `POST /api/areas` retorna `201` com header `Location` apontando para o recurso criado.
- [ ] Slug duplicado (contra área ativa ou inativa) retorna `409` de forma controlada, nunca um erro não tratado (`500`) em fluxo sem concorrência.
- [ ] Área inexistente em `GET /{id}` ou `PUT /{id}` retorna `404`.
- [ ] Payload inválido (`Name`/`Description`/`Slug`/`DisplayOrder` fora dos limites) retorna `400`.
- [ ] Requisição sem JWT retorna `401`; com JWT mas sem permissão retorna `403`.
- [ ] Nenhum endpoint novo acessa `CourseCoreDbContext` ou repository EF concreto diretamente a partir do controller.
- [ ] Toda escrita é atômica (nenhum estado parcial fica persistido em caso de falha de validação ou conflito).
- [ ] Criação e atualização geram os eventos de auditoria correspondentes, sem gravar dado sensível (não há campo sensível em Area hoje).
- [ ] Os consumidores existentes da listagem de Areas (`CourseAccessService`) continuam funcionando sem alteração de comportamento e sem quebra de compilação.
- [ ] Testes de integração cobrem: criação e leitura via API pública, atualização com desativação, conflito de slug, `400`/`401`/`403`/`404`, e que os endpoints aparecem como protegidos por Bearer no OpenAPI.
- [ ] `dotnet build` e `dotnet test` passam sem regressão.
- [ ] Nenhuma migration é criada (§11 Decisão 1 confirma que o schema não muda nesta etapa).

## 10. Fora de escopo

- Remoção física (`DELETE`) de Area.
- CRUD de `Role` ou `Permission`.
- Atribuição de permissões a roles (não existe use case para isso hoje; `areas.manage` continua só utilizável por quem já tem a permissão seedada ou é `Admin`).
- Atribuição de roles a usuários.
- Concessão/edição de `UserAreaAccess` ou `RoleAreaAccess` (endpoints já existentes em `/api/access`, não tocados por esta spec).
- Paginação da listagem (dataset de baixa cardinalidade; pode ser adicionada depois sem quebrar o contrato atual).
- CRUD de Course, Module ou Lesson.
- Vínculo automático de acesso (`UserAreaAccess`/`RoleAreaAccess`) ao criar uma Area — criar uma Area **não** concede acesso a ninguém automaticamente.
- Qualquer alteração de schema/migration além do estritamente necessário (hoje: nenhuma).
- Hardening de concorrência (lock otimista, tratamento de violação de índice único) além do que já existe no restante do projeto (decisão confirmada, §11 Decisão 3).
- Qualquer bloqueio, aviso ou confirmação adicional ao desativar uma Area com cursos/grants vinculados (decisão confirmada, §11 Decisão 4).
- Índice único parcial/filtrado por `Active` em `Slug` (decisão confirmada, §11 Decisão 1).
- Mecanismo interno de implementação (nomes de exceptions, assinatura exata de métodos de repositório, filtro aplicado no repositório vs. em memória no use case) — cabe à implementação, respeitando o contrato HTTP e as regras de negócio definidas aqui.

## 11. Decisões

Decisões de negócio que o código existente não resolvia sozinho. Todas resolvidas em 2026-09-01 — mantidas aqui como registro histórico.

1. **Reuso de slug após desativação.** ✅ Resolvida: mantém unicidade global de `Slug` como está hoje, sem índice parcial e sem migration. Uma Area desativada mantém seu slug reservado indefinidamente; reuso exige reativar ou renomear a Area existente.
2. **Escopo de leitura vs. escrita na policy.** ✅ Resolvida: os 4 endpoints (`GET`/`POST`/`PUT`) ficam todos atrás de `ManageAreas`, sem exceção para `courses.manage` ou usuários autenticados em geral.
3. **Nível de proteção contra corrida em criação simultânea de slug duplicado.** ✅ Resolvida: mantém o padrão check-then-insert já usado em todo o projeto, sem tratamento explícito de violação de índice único. Corrida rara pode resultar em `500` em vez de `409` — aceito.
4. **Efeito de desativar uma Area sobre acesso já concedido.** ✅ Resolvida: apenas documentado (Regra de negócio 11); nenhum bloqueio ou aviso adicional é implementado nesta etapa.
