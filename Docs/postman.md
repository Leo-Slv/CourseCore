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

A senha deve ter ao menos 12 caracteres e no maximo 72 bytes UTF-8, limite seguro do BCrypt. Ela nao pode ser vazia nem uma senha comum basica. A mesma politica vale para usuarios criados por `Users / Create User`.

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

## Verificacao de acesso a cursos

Use `Access / Check Own Course Access` (`GET /api/access/courses/{courseId}`) para consultar o acesso do usuario autenticado. Essa rota usa exclusivamente o `userId` do JWT e exige apenas autenticacao.

Use `Access / Check User Course Access (Administrative)` (`GET /api/access/users/{userId}/courses/{courseId}`) para consultar outro usuario. Essa rota exige `users.manage`, `areas.manage`, `courses.manage` ou a role `Admin`.

O endpoint `POST /api/access/course/check` permanece apenas para compatibilidade e esta deprecated. Ele nunca aceita `userId` alvo e sempre consulta o proprio usuario autenticado.

Os grants usam policies separadas:

```text
POST /api/access/user-area -> users.manage ou Admin
POST /api/access/role-area -> roles.manage ou Admin
```

Uma permissao nao autoriza o grant da outra categoria. Grants para roles inativas retornam `409 Conflict`.

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

Payloads administrativos possuem limites de tamanho. Create Course aceita no maximo 50 areas, 50 modulos e 100 aulas por modulo; URLs de thumbnail devem ser HTTP(S) absolutas. O corpo HTTP e limitado a 1 MiB. Payload invalido retorna `400`, sem detalhes internos.

- Nao versionar senha real.
- Nao versionar JWT real.
- Nao versionar refresh token real.
- Nao versionar connection string real.
- Nao copiar valores reais de `.env` para a collection.
- Docker Compose nao aplica migrations automaticamente.
- `/health/ready` pode falhar enquanto o schema do banco nao estiver aplicado.
- Rate limiting nao substitui MFA, CAPTCHA ou lockout futuro.
- JWT antigo pode ser rejeitado imediatamente apos mudancas criticas de usuario.
