# Postman

Este guia explica como usar a colecao Postman da CourseCore API para testar os endpoints HTTP principais.

## Arquivos

Importe estes arquivos no Postman:

```text
Postman/CourseCore.postman_collection.json
Postman/CourseCore.local.postman_environment.json
```

Selecione o environment `CourseCore Local` antes de executar as requests.

## Base URL

O environment usa Docker por padrao:

```text
baseUrl=http://localhost:8080
```

Para rodar sem Docker, troque `baseUrl` para a porta usada pelo profile local da API. Em geral, ela aparece em `Properties/launchSettings.json` ou no console do `dotnet run`, por exemplo:

```text
http://localhost:5278
```

Scalar e OpenAPI ficam disponiveis apenas em `Development`.

## Seed admin

A collection usa o admin esperado do seed:

```text
adminEmail=admin@coursecore.local
adminPassword=CHANGE_ME_LOCAL_ONLY
```

Antes de usar a collection, configure `adminPassword` no environment com a senha local que voce definiu para o seed. Nao salve senha real em arquivo versionado.

O seed admin e opt-in, roda somente em `Development` e exige schema atualizado. Para habilitar localmente:

```powershell
$env:Seed__Admin__Enabled="true"
$env:Seed__Admin__Name="CourseCore Admin"
$env:Seed__Admin__Email="admin@coursecore.local"
$env:Seed__Admin__Password="CHANGE_ME_LOCAL_ONLY"
$env:Seed__Admin__ResetPassword="false"
dotnet run
```

Veja tambem `Docs/database-seeding.md`.

## Fluxo recomendado

1. Suba a API com PostgreSQL e migrations aplicadas.
2. Importe a collection e o environment.
3. Preencha `adminPassword`.
4. Execute `Auth / Login as Seed Admin`.
5. Use os endpoints protegidos.
6. Quando necessario, execute `Auth / Refresh Token` para renovar o access token usando o cookie jar do Postman.
7. Execute `Auth / Logout` para revogar o refresh token da sessao atual e limpar o cookie.

A request de login salva automaticamente:

```text
accessToken
```

O refresh token nao e salvo no environment. A API envia o refresh token em cookie `HttpOnly`, e o Postman usa o cookie jar automaticamente nas requests seguintes.

A request de refresh token atualiza o `accessToken` e recebe um novo cookie de refresh token.

A request de logout usa o cookie do Postman, espera `204 No Content` e limpa o cookie no servidor.

## Variaveis do environment

Variaveis preenchidas pela collection:

```text
accessToken
correlationId
createdUserId
courseId
moduleId
lessonId
videoId
```

Variaveis que normalmente precisam ser preenchidas manualmente ou obtidas por seed/banco:

```text
adminPassword
areaId
roleId
```

`areaId` e necessario para criar cursos e conceder acesso por area. `roleId` e necessario para conceder acesso por role. Se voce criar um curso pela collection, rode depois `Courses / Get Course Details` para tentar salvar `moduleId` e `lessonId` a partir da resposta.

## Authorization

Os folders protegidos usam:

```text
Bearer {{accessToken}}
```

O access token inclui a claim `token_version`. Se o usuario for desativado ou sofrer atualizacao critica, tokens antigos passam a receber `401 Unauthorized`; faca login novamente com um usuario ativo.

Login, refresh token e logout sao publicos e nao usam Bearer.

Login, refresh token e logout usam cookie `HttpOnly` para o refresh token. Se voce estiver testando um cliente mobile ou uma ferramenta sem cookie jar, o fallback por body pode ser habilitado por configuracao local com `Auth__AllowRefreshTokenInBodyFallback=true`. Em producao, mantenha o fallback desabilitado salvo decisao operacional explicita.

Esses endpoints possuem rate limiting por IP. Excesso de tentativas retorna `429 Too Many Requests`, possivelmente com `Retry-After`. Se uma runner da collection executar muitas chamadas rapidamente, aguarde a janela configurada ou ajuste apenas a configuracao local de rate limit.

Para frontends web/PWA, mantenha o access token apenas em memoria no cliente. O navegador envia o cookie automaticamente para `/api/auth`.

Se frontend e API forem cross-site, sera necessario avaliar CORS com credentials, origem explicita e `SameSite=None; Secure`. Esta etapa nao habilita `AllowCredentials` nem abre CORS. Protecao CSRF completa fica como pendencia futura para fluxos com cookie.

## Progresso

A request `Progress / Register Lesson Progress` deve enviar `watchedSeconds` suficiente para atingir o threshold configurado no servidor. Por padrao, a aula conclui ao atingir 90% de `Video.DurationSeconds`.

O campo `markAsCompleted` pode existir em clientes antigos, mas esta deprecated e e ignorado. A collection nao depende mais dele; conclusao de aula e curso e calculada pela API.

## Videos

A request `Media / Videos / Create Video` nao envia mais `playbackUrl`. O campo ainda e aceito por compatibilidade em clientes antigos, mas esta deprecated e e ignorado pela API.

Depois de criar um video, execute `Media / Videos / Mark Video Ready` para alterar o status para `Ready` sem informar URL. `Request Video Playback` retorna uma URL temporaria assinada e `expiresAt`; nao salve essa URL como variavel permanente.

## Correlation id

A collection possui um pre-request script global que gera um novo GUID por request e salva em:

```text
correlationId
```

Todas as requests enviam:

```text
X-Correlation-ID: {{correlationId}}
```

As respostas tambem devem devolver esse header.

## Cuidados

- Nao versionar senha real.
- Nao versionar JWT real.
- Nao versionar refresh token real.
- Nao versionar connection string real.
- Nao copiar valores reais de `.env` para a collection.
- Docker Compose nao aplica migrations automaticamente.
- `/health/ready` pode falhar enquanto o schema do banco nao estiver aplicado.
- Rate limiting nao substitui MFA, CAPTCHA ou lockout futuro.
- JWT antigo pode ser rejeitado imediatamente apos mudancas criticas de usuario.
