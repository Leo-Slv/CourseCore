# Spec — Migrar leituras expostas como POST para GET

**Status:** Approved
**Aprovada em:** 2026-09-02

## 1. Objetivo

Corrigir dois endpoints que expõem operações de **leitura pura** (sem efeito colateral, idempotentes) através do verbo `POST` com o identificador no corpo da requisição, quando deveriam ser `GET` com o identificador na rota:

- `POST /api/progress/courses` (body `{ courseId }`) → progresso do usuário autenticado num curso.
- `POST /api/videos/playback` (body `{ videoId }`) → URL de playback assinada para um vídeo.

Isso corrige uma inconsistência de convenção HTTP/REST: essas operações não mutam nenhum estado do servidor, então deveriam ser seguras e cacheáveis como toda leitura, e identificáveis por URL — hoje não são, porque usam `POST`.

Os dois `POST` são **substituídos**, não deprecados: a aplicação está em desenvolvimento, sem usuários nem consumidores externos, então não há custo de compatibilidade a proteger (ver §11, Decisão 1).

## 2. Contexto

### 2.1 Por que isso importa, não é só estilo

- `GET` é a única forma padronizada de dizer a proxies, CDNs, navegadores e ferramentas HTTP que uma chamada é segura (não muda nada) e idempotente. Com `POST`, esses intermediários assumem o oposto por padrão.
- Um corpo JSON em `POST` força preflight de CORS (`OPTIONS`) em navegadores para esses dois endpoints; um `GET` sem headers customizados normalmente não precisa.
- A URL de um `GET` identifica o recurso sozinha — pode ser copiada, logada, testada direto no navegador ou em ferramentas de linha de comando sem montar corpo JSON.

### 2.2 Precedente existente, e por que esta spec não o repete

`POST /api/access/course/check` (body `{ courseId }`) foi deprecado e substituído por `GET /api/access/courses/{courseId}` — ver [AreasController.cs:73-107](../../../Modules/Access/Presentation/Controllers/AreasController.cs). O endpoint antigo continua funcionando, está marcado `[Obsolete("Use GET /api/access/courses/{courseId} instead.")]`, e foi movido para a pasta `90 - Deprecated` da collection Postman (ver [postman.md](../../postman.md)). Esse precedente confirma que `GET` com id na rota é a forma correta — é o que embasa a §3 desta spec.

Esta spec **não** repete o tratamento aditivo (`[Obsolete]` + manter o `POST` funcionando) daquele precedente. Decisão confirmada (§11, Decisão 1): como a aplicação está em desenvolvimento, sem usuários nem consumidores externos, os dois `POST` são substituídos diretamente pelos `GET` novos — sem período de convivência, sem depreciação. O princípio registrado em `Docs/coursecore-course-authoring-essential-features-plan.md` ("Endpoints existentes só mudam com estratégia explícita de compatibilidade") continua respeitado: a estratégia explícita, para este caso, é corte direto.

### 2.3 O que já existe e não muda

As regras de negócio de ambos os endpoints já vivem inteiramente na camada de Application e **não são alteradas por esta spec** — só a camada de Presentation (rota, verbo, request) muda:

- [GetCourseProgressUseCase.cs](../../../Modules/Progress/Application/UseCases/GetCourseProgressUseCase.cs) — valida usuário e curso existentes, checa acesso via `CourseAccessService.CanUserAccessCourseAsync`, retorna progresso vazio se o usuário nunca registrou nada no curso.
- [RequestVideoPlaybackUseCase.cs](../../../Modules/Media/Application/UseCases/RequestVideoPlaybackUseCase.cs) — valida usuário ativo, vídeo/aula/curso existentes, vídeo com `Status == Ready`, acesso ao curso via `CourseAccessService`, e só então gera a URL assinada com `IVideoStorageService.GeneratePlaybackUrlAsync`.

Ambos os use cases já recebem `userId` (do token, via `ICurrentUserService`) e o id do recurso como entrada simples — nenhum dos dois precisa de mais nenhum campo do corpo além do id que já está sendo migrado para a rota (confirmado em [GetCourseProgressRequest.cs](../../../Modules/Progress/Presentation/Requests/GetCourseProgressRequest.cs) e [RequestVideoPlaybackRequest.cs](../../../Modules/Media/Presentation/Requests/RequestVideoPlaybackRequest.cs), que só têm o campo do id).

## 3. Comportamento esperado

| Recurso | Endpoint atual | Novo endpoint | Sucesso |
|---|---|---|---|
| Progresso em curso | `POST /api/progress/courses` (body `courseId`) | `GET /api/progress/courses/{courseId}` | `200` + `CourseProgressResponse` |
| Playback de vídeo | `POST /api/videos/playback` (body `videoId`) | `GET /api/videos/{videoId}/playback` | `200` + `VideoPlaybackResponse` |

O formato de resposta (`CourseProgressResponse`, `VideoPlaybackResponse`) não muda. O comportamento de negócio (validações, ordem de checagem, mensagens de erro) não muda — só o transporte HTTP.

Os dois endpoints antigos (`POST`) são removidos nesta mudança: `POST /api/progress/courses` e `POST /api/videos/playback` deixam de existir, substituídos pelos `GET` correspondentes. Diferente do precedente de `POST /api/access/course/check`, não há período de convivência nem `[Obsolete]` — decisão confirmada em §11.

## 4. Regras de negócio

1. O novo `GET` deve produzir exatamente a mesma resposta e os mesmos códigos de erro que o `POST` que ele substitui produzia para a mesma entrada — a migração é de transporte, não de comportamento.
2. Nenhuma regra de autorização muda: `GET /api/progress/courses/{courseId}` continua exigindo usuário autenticado (sem policy nomeada, igual hoje); `GET /api/videos/{videoId}/playback` continua sem policy nomeada além de autenticação — a checagem de acesso ao curso continua sendo feita pelo use case via `CourseAccessService`, não pela policy do controller.
3. As respostas de ambos os endpoints carregam dado privado por usuário (progresso pessoal) ou sensível por natureza (URL assinada com prazo de expiração). Ambos os `GET`s devem responder com `Cache-Control: no-store` para impedir que proxy, CDN ou o próprio navegador armazenem essas respostas — comportamento que `POST` já obtinha "de graça" (POST não é cacheável por padrão), e que o `GET` precisa declarar explicitamente para não regredir.
4. O endpoint antigo (`POST`) é removido, não deprecado — decisão confirmada em §11: sem usuários ou consumidores externos, manter os dois endpoints em paralelo não traz benefício e só duplica superfície de manutenção/teste.

## 5. Pré-condições

- Requisição autenticada (`[Authorize]` de classe, já existente em ambos os controllers).
- `courseId`/`videoId` sintaticamente válidos como GUID na rota — mesma convenção `{param:guid}` já usada em `CoursesController`/`AreasController`/`AreaManagementController`.

## 6. Fluxo principal

### 6.1 Progresso em curso

1. O cliente chama `GET /api/progress/courses/{courseId}` autenticado.
2. O sistema identifica o usuário pelo token (nunca por um id enviado pelo cliente).
3. O sistema valida que o usuário e o curso existem e que o usuário tem acesso ao curso.
4. O sistema retorna o progresso agregado do curso e de cada aula; se o usuário nunca registrou progresso nesse curso, retorna um progresso vazio (não um erro).

### 6.2 Playback de vídeo

1. O cliente chama `GET /api/videos/{videoId}/playback` autenticado.
2. O sistema identifica o usuário pelo token.
3. O sistema valida usuário ativo, vídeo/aula/curso existentes, vídeo pronto (`Ready`) e acesso do usuário ao curso.
4. O sistema gera e retorna uma URL de playback assinada, com prazo de expiração.

## 7. Cenários de erro

| Cenário | Endpoint(s) | HTTP |
|---|---|---|
| Sem JWT / token inválido | ambos | `401` |
| `courseId`/`videoId` sintaticamente inválido (não é GUID) | ambos | `404` (não casa com a rota, mesma convenção já usada no projeto) |
| Usuário do token não encontrado | ambos | `404` |
| Curso/vídeo/aula não encontrado | ambos | `404` |
| Usuário sem acesso ao curso | ambos | `403` |
| Usuário inativo | playback | `403` |
| Vídeo existe mas não está `Ready` | playback | `409` |
| Erro inesperado não coberto acima | ambos | `500` |

Nenhum desses comportamentos é novo — são os mesmos já produzidos pelos use cases hoje; a tabela só documenta que continuam idênticos após a migração de verbo.

## 8. Casos de borda

- **Chamar o endpoint antigo (`POST`) depois da migração**: deve continuar funcionando exatamente como hoje, sem aviso além do que o cliente HTTP/OpenAPI já mostra para `[Obsolete]`.
- **Progresso nunca registrado no curso**: `GET /api/progress/courses/{courseId}` retorna `200` com progresso zerado, não `404` — comportamento já existente, só confirmando que não muda.
- **Cliente que dependia do corpo JSON do `POST`**: ao chamar o `GET` novo enviando corpo, o corpo é ignorado (GET não tem contrato de request body nesta API); o `courseId`/`videoId` da rota é a única fonte de verdade.
- **Vídeo com `Status` diferente de `Ready` acessado via o `GET` novo**: mesmo `409` que o `POST` já retorna hoje — não vira `404` nem `400`.

## 9. Critérios de aceite

- [ ] `GET /api/progress/courses/{courseId}` existe, autenticado, e retorna exatamente o mesmo `CourseProgressResponse` que `POST /api/progress/courses` retornaria para a mesma entrada.
- [ ] `GET /api/videos/{videoId}/playback` existe, autenticado, e retorna exatamente o mesmo `VideoPlaybackResponse` que `POST /api/videos/playback` retornaria para a mesma entrada.
- [ ] Os dois `POST` antigos continuam respondendo `200` para chamadas válidas e são marcados `[Obsolete]` apontando para o `GET` correspondente, como `POST /api/access/course/check` já é.
- [ ] Ambos os `GET`s novos respondem com `Cache-Control: no-store`.
- [ ] Todos os cenários de erro da tabela da seção 7 têm cobertura de teste para o `GET` novo (reaproveitando os cenários que já existem para o `POST`, ver [ProgressIntegrationTests.cs](../../../Tests/CourseCore.Api.Tests/Integration/Progress/ProgressIntegrationTests.cs) e [VideosIntegrationTests.cs](../../../Tests/CourseCore.Api.Tests/Integration/Media/VideosIntegrationTests.cs)).
- [ ] OpenAPI documenta os dois `GET`s novos como protegidos por Bearer, seguindo o padrão de teste já usado em [OpenApiIntegrationTests.cs](../../../Tests/CourseCore.Api.Tests/Integration/OpenApi/OpenApiIntegrationTests.cs) (que já teria `/api/videos/playback` como `post` e precisa ganhar a entrada `get` para o playback e para progress).
- [ ] Postman: os dois `GET`s novos substituem os `POST`s nas pastas operacionais (`05 - Media / Videos`, `06 - Progress`); os `POST`s antigos são movidos para `90 - Deprecated`, mesmo tratamento de `Check Own Course Access - Deprecated`.
- [ ] `dotnet build` e `dotnet test` passam sem regressão.

## 10. Fora de escopo

- Qualquer mudança nas regras de negócio, validações ou mensagens de erro dos dois fluxos — só o transporte HTTP muda.
- Remoção dos endpoints `POST` antigos.
- Migração de qualquer outro endpoint além destes dois (ex.: `POST /api/access/user-area`/`role-area`, identificados como "melhoria opcional, não erro" na análise anterior, ficam de fora).
- Mudança de policy de autorização de qualquer um dos dois endpoints.
- Versionamento de API (`/v2`, header de versão, etc.) — a migração é aditiva dentro da mesma versão de rota, igual ao precedente de `access/course/check`.

## 11. Decisões

Decisões de negócio que o código e o precedente existente não resolvem sozinhos:

1. **Estratégia de migração: aditiva (manter `POST` deprecado) ou corte direto (remover `POST` já nesta mudança)?** O precedente do projeto (`access/course/check`) e o princípio já documentado em outro plano apontam para aditiva — é o que esta spec assume por padrão (seção 2.2, 3, 4). Preciso de confirmação explícita, já que só quem conhece se existem consumidores externos desses dois endpoints específicos em produção pode decidir se um corte direto é aceitável aqui.
2. **Prazo de descontinuação dos `POST`s antigos.** O precedente (`access/course/check`) não tem data de remoção definida — está deprecado indefinidamente. Esta spec assume o mesmo (sem prazo) para os dois novos casos, a menos que você quera definir uma janela de remoção explícita agora.
