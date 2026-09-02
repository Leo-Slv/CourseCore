# Implementation Plan — Migrar leituras expostas como POST para GET

**Spec:** [Docs/specs/api-conventions/get-for-read-endpoints.md](get-for-read-endpoints.md) (Approved, 2026-09-02)
**Status:** Implemented (2026-09-02)

Este documento é o **HOW**: como implementar o corte direto definido na spec (remover `POST /api/progress/courses` e `POST /api/videos/playback`, substituindo por `GET` com o id na rota), seguindo `.claude/claude.md` e os padrões já existentes em `CoursesController`/`VideosController` para actions que recebem só um id de rota.

## 1. Padrão já existente a reaproveitar (achado central deste plano)

`CoursesController.PublishAsync` e `VideosController.MarkReadyAsync` já são o precedente exato do que os dois `GET` novos precisam ser: uma action que recebe só um `Guid` de rota, monta o Input diretamente (`new PublishCourseInput { CourseId = courseId }`, `new MarkVideoReadyInput { VideoId = id }`) **sem** Request DTO nem Presenter — porque não há nada para desserializar de um corpo que não existe.

```csharp
// VideosController.cs:66-75, padrão a copiar
public async Task<ActionResult<VideoResponse>> MarkReadyAsync(Guid id, CancellationToken cancellationToken)
{
    var output = await _markVideoReadyUseCase.ExecuteAsync(new MarkVideoReadyInput { VideoId = id }, cancellationToken);
    return Ok(VideoPresenter.ToResponse(output));
}
```

Isso significa que os dois `GET` novos **não precisam de Request DTO novo** — eles substituem `GetCourseProgressRequest`/`RequestVideoPlaybackRequest` (que só tinham o campo do id) por um parâmetro de rota `Guid`, e constroem `GetCourseProgressInput`/`RequestVideoPlaybackInput` diretamente no controller, com `UserId` vindo de `GetCurrentUserId()` (já existente em ambos os controllers) e o id vindo da rota.

## 2. O que muda

### 2.1 `Modules/Progress/Presentation/Controllers/ProgressController.cs`

Substituir a action `GetCourseProgressAsync`:

- Remover `[HttpPost("courses")]` e o parâmetro `GetCourseProgressRequest request`.
- Adicionar `[HttpGet("courses/{courseId:guid}")]` com parâmetro `Guid courseId`.
- Adicionar `[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]` (ver §3 — cobre a Regra de negócio 3 da spec, `Cache-Control: no-store`).
- Construir o input diretamente: `new GetCourseProgressInput { UserId = GetCurrentUserId(), CourseId = courseId }`, sem usar `ProgressPresenter.ToInput(Guid, GetCourseProgressRequest)`.
- `RegisterLessonProgressAsync` (`POST /api/progress/lessons`) não muda — continua sendo uma escrita.

### 2.2 `Modules/Media/Presentation/Controllers/VideosController.cs`

Substituir a action `RequestPlaybackAsync`:

- Remover `[HttpPost("playback")]` e o parâmetro `RequestVideoPlaybackRequest request`.
- Adicionar `[HttpGet("{videoId:guid}/playback")]` com parâmetro `Guid videoId`.
- Adicionar `[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]`.
- Construir o input diretamente: `new RequestVideoPlaybackInput { UserId = GetCurrentUserId(), VideoId = videoId }`, sem usar `VideoPresenter.ToInput(Guid, RequestVideoPlaybackRequest)`.
- Atenção à rota: `CreateAsync` é `[HttpPost]` na raiz (`api/videos`) e `MarkReadyAsync` já usa `{id:guid}/ready`. A nova rota `{videoId:guid}/playback` não colide com nenhuma delas, mas usa `videoId` como nome de parâmetro (não `id`, para ficar explícito — `MarkReadyAsync` usa `id` por herança do diagrama original, não precisa ficar consistente com isso).
- `CreateAsync` e `MarkReadyAsync` não mudam.

### 2.3 Remover código morto

Depois que as duas actions acima não usarem mais Request DTO nem Presenter para esse fluxo:

- Apagar `Modules/Progress/Presentation/Requests/GetCourseProgressRequest.cs`.
- Apagar `Modules/Media/Presentation/Requests/RequestVideoPlaybackRequest.cs`.
- Remover `ProgressPresenter.ToInput(Guid userId, GetCourseProgressRequest request)` de [ProgressPresenter.cs](../../../Modules/Progress/Presentation/Presenters/ProgressPresenter.cs) (mantém os outros métodos).
- Remover `VideoPresenter.ToInput(Guid userId, RequestVideoPlaybackRequest request)` de [VideoPresenter.cs](../../../Modules/Media/Presentation/Presenters/VideoPresenter.cs) (mantém os outros métodos).

Não mexer em `GetCourseProgressInput`, `RequestVideoPlaybackInput`, nem em nenhum use case — a spec (§2.3) já confirma que a camada de Application não muda.

## 3. Cache-Control: no-store

Não existe nenhum precedente de `Cache-Control` no projeto (`grep` em `Modules/` e `Shared/` não encontrou nada). Mecanismo escolhido: o atributo nativo do ASP.NET Core `[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]` nas duas actions.

Por que essa opção e não outra:

- É um `IFilterMetadata`/`ResponseCacheFilter` que escreve o header `Cache-Control` na resposta, independente de `app.UseResponseCaching()` estar registrado — não precisa adicionar middleware novo nem serviço novo.
- Não introduz padrão novo de arquitetura (nada em `Shared/`); é um atributo declarativo por action, no mesmo espírito de `[ProducesResponseType]` que todo controller já usa.
- Alternativa descartada: setar `Response.Headers` manualmente dentro da action — funciona, mas é imperativo e cada desenvolvedor futuro escreveria diferente; o atributo é a forma idiomática e documentada do framework.

`using Microsoft.AspNetCore.Mvc;` já está presente em ambos os controllers (é de onde vem `[ApiController]`), então nenhum using novo é necessário.

## 4. Testes

### 4.1 `Tests/CourseCore.Api.Tests/Integration/Progress/ProgressIntegrationTests.cs`

3 chamadas em `client.PostAsJsonAsync("/api/progress/courses", new { courseId = ... })` (linhas 171, 184, 199) viram `client.GetAsync($"/api/progress/courses/{courseId}")`. Migrar os cenários existentes (não duplicar): sucesso, curso sem progresso registrado, curso sem acesso, etc. — o que já é coberto para o `POST` passa a cobrir o `GET`.

### 4.2 `Tests/CourseCore.Api.Tests/Integration/Media/VideosIntegrationTests.cs`

5 chamadas em `client.PostAsJsonAsync("/api/videos/playback", new { videoId = ... })` (linhas 110, 172, 185, 202, 217) viram `client.GetAsync($"/api/videos/{videoId}/playback")`. Mesma lógica: migrar os cenários (sucesso, vídeo não pronto → 409, sem acesso → 403, vídeo/usuário inexistente → 404), sem duplicar.

### 4.3 `Tests/CourseCore.Api.Tests/Integration/OpenApi/OpenApiIntegrationTests.cs`

Linha 88: `[InlineData("/api/videos/playback", "post")]` vira `[InlineData("/api/videos/{videoId}/playback", "get")]`. Adicionar `[InlineData("/api/progress/courses/{courseId}", "get")]` — confirmado que não existe nenhuma entrada para `progress/courses` neste arquivo hoje (só há `[InlineData("/api/progress/lessons", "post")]`, linha 89), então é uma entrada genuinamente nova, não uma migração.

### 4.4 Novo teste de `Cache-Control`

Nenhum teste no projeto hoje verifica header `Cache-Control`. Adicionar uma asserção nos dois testes de sucesso (progress e playback) confirmando `response.Headers.CacheControl.NoStore == true` (ou verificação equivalente do header bruto) — cobre a Regra de negócio 3 e o critério de aceite correspondente da spec.

### 4.5 Teste de rota removida

Adicionar um teste (um em cada arquivo de integração, ou um teste combinado) confirmando que `POST /api/progress/courses` e `POST /api/videos/playback` retornam `404` — cobre o critério de aceite "chamá-los retorna 404 de rota não encontrada".

## 5. Documentação

### 5.1 Postman

Em `Postman/CourseCore.postman_collection.json`:

- `05 - Media / Videos` → `Get Playback Url`: método `POST` → `GET`; URL `{{baseUrl}}/api/videos/playback` → `{{baseUrl}}/api/videos/{{videoId}}/playback`; remover o `body` (não existe mais); remover o header `Content-Type` (GET sem corpo não precisa).
- `06 - Progress` → `Get Course Progress`: método `POST` → `GET`; URL `{{baseUrl}}/api/progress/courses` → `{{baseUrl}}/api/progress/courses/{{courseId}}`; remover `body` e header `Content-Type`.
- Nenhuma entrada nova em `90 - Deprecated` (decisão de corte direto — diferente do que foi feito para `access/course/check`).

### 5.2 `Docs/postman.md`

Atualizar a tabela de inventário de endpoints (linhas dos dois endpoints: método/rota/entrada mudam de `POST`/body para `GET`/path). Atualizar a contagem total de endpoints executáveis se ela mudar (hoje 28; a troca de verbo não muda a contagem, mas remover o corpo pode valer uma nota).

### 5.3 `Docs/implementation-class-diagram.md`

Confirmado: nenhuma mudança necessária. O diagrama já documenta `RequestPlaybackAsync(Guid videoId) Task<VideoPlaybackResponse>` e `GetCourseProgressAsync(Guid courseId) Task<CourseProgressResponse>` (linhas 45 e 50) — só a assinatura C#, sem verbo/rota HTTP, e já usando `Guid` como parâmetro direto (nunca documentou um Request DTO com o id no corpo). O diagrama já refletia a forma que o código está migrando para.

### 5.4 README.md

Confirmado: nenhuma mudança necessária. `README.md` não cita `/api/progress/courses` nem `/api/videos/playback` em nenhum lugar.

## 6. Sequência recomendada

1. `ProgressController` + remoção de `GetCourseProgressRequest`/`ProgressPresenter.ToInput` correspondente.
2. `VideosController` + remoção de `RequestVideoPlaybackRequest`/`VideoPresenter.ToInput` correspondente.
3. Testes de integração (Progress, Videos, OpenAPI) — migrar chamadas existentes, adicionar teste de `Cache-Control` e de `404` na rota antiga.
4. Postman + `Docs/postman.md`.
5. Checar (não assumir) `implementation-class-diagram.md` e `README.md`.
6. Validação completa (§7).

## 7. Validação obrigatória

```powershell
dotnet build
dotnet test
```

```powershell
Get-Content Postman/CourseCore.postman_collection.json -Raw | ConvertFrom-Json | Out-Null
Get-Content Postman/CourseCore.local.postman_environment.json -Raw | ConvertFrom-Json | Out-Null
```

Confirmar explicitamente:

- nenhuma regra de negócio dos dois use cases foi tocada (só Presentation mudou);
- `POST /api/progress/courses` e `POST /api/videos/playback` retornam `404`;
- os dois `GET` novos respondem `Cache-Control: no-store`;
- nenhuma migration foi criada (não há mudança de schema nesta feature);
- nenhum secret foi adicionado.

## 8. Fora deste plano

Idêntico ao "Fora de escopo" da spec (§10): nenhuma mudança de regra de negócio, nenhuma manutenção do `POST` antigo, nenhuma mudança em `POST /api/access/user-area`/`role-area` ou em `POST /api/access/course/check`, nenhum versionamento de API.
