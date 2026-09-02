# Postman

A collection do CourseCore cobre os 31 endpoints executáveis da API: 28 actions de controllers e 3 health checks. Ela também inclui 2 requests de diagnóstico disponíveis apenas em `Development` e 8 cenários negativos manuais.

## Importação e configuração

Importe e selecione:

```text
Postman/CourseCore.postman_collection.json
Postman/CourseCore.local.postman_environment.json
```

O environment versionado contém somente placeholders. Ajuste `baseUrl` para a URL efetiva da API, sem barra final. O valor inicial é:

```text
http://localhost:5000
```

Em Docker, use normalmente `http://localhost:8080`. Sem Docker, consulte `Properties/launchSettings.json` ou a saída de `dotnet run`.

Preencha manualmente antes do fluxo correspondente:

- `adminEmail` e `adminPassword`: credenciais administrativas locais;
- `studentEmail` e `studentPassword`: credenciais sem permissões administrativas, usadas em fluxos de aluno e no cenário 403;
- `roleId`: não há endpoint público para criar ou listar roles;
- `areaId`: só é necessário preencher manualmente se `03 - Areas` não for executado antes; `List Areas` e `Create Area` o preenchem automaticamente;
- `targetUserId`, `courseId`, `lessonId` e `videoId`: somente quando o recurso não puder ser obtido por um request anterior;
- `videoStorageKey`: chave válida no provider configurado;
- `captchaToken`: fora de `Production`, com `Turnstile:SecretKey` vazio no servidor, qualquer valor (inclusive o placeholder `dev-bypass`) é aceito — o servidor pula a verificação; em `Production`, precisa ser uma resposta real do Turnstile;
- `emailVerificationToken`: cole aqui o token recebido no e-mail (ou, sem Resend configurado, o texto logado pelo servidor) antes de rodar `Confirm Email`.

Nunca salve credenciais reais, JWT, refresh token, cookie ou connection string nos arquivos versionados.

## Autenticação automática

A collection usa Bearer `{{accessToken}}` globalmente. Health, login, refresh, logout e diagnósticos sobrescrevem a autenticação com `No Auth`.

`Login Admin` envia `adminEmail`/`adminPassword`, lê o contrato real `token.accessToken`, salva o JWT em `accessToken` e salva `userId`. `Login Student` faz o mesmo em `studentAccessToken`, usado explicitamente pelo cenário negativo de autorização.

`Register` cria uma conta pública nova (sem role nenhuma) e já autentica no mesmo request, igual a um login — salva `accessToken` e `userId`. A conta nasce com e-mail não confirmado: `Confirm Email`/`Resend Confirmation` ficam na mesma pasta para fechar esse fluxo. Enquanto o e-mail não é confirmado, nenhum curso é acessível (nem gratuito) — só o catálogo (`List Available Courses`) continua visível.

O refresh token não é salvo em variável. Login/Register gravam o refresh token em cookie `HttpOnly`; o cookie jar do Postman o envia automaticamente a `Refresh Token` e `Logout`. O body mantém `refreshToken` vazio apenas por compatibilidade com o contrato. `Refresh Token` salva o novo `token.accessToken` em `accessToken`. Após `200` ou `204`, `Logout` remove `accessToken` e `studentAccessToken` do environment; a API apaga o cookie.

Login, register, refresh, logout e reenvio de confirmação têm rate limiting. Não há loop agressivo de 429 na collection.

## Ordem sugerida no Collection Runner

As pastas numeradas expressam a ordem operacional:

1. `00 - Health`;
2. `01 - Auth` — execute `Login Admin`; `Login Student` é opcional; `Register`/`Confirm Email`/`Resend Confirmation` são um fluxo à parte, para testar o cadastro público;
3. `02 - Users`;
4. `03 - Access`;
5. `03 - Areas` — `List Areas` e `Create Area` preenchem `areaId` para as pastas seguintes;
6. `04 - Courses`;
7. `05 - Media / Videos`;
8. `06 - Progress`;
9. `90 - Deprecated` apenas quando necessário;
10. `95 - Negative Scenarios` individualmente, após preparar suas dependências;
11. `98 - Session Cleanup`;
12. `99 - Diagnostics` apenas em `Development`.

O Runner cria areas via `03 - Areas`, mas não cria roles, pois a API não expõe endpoint para isso. Fluxos de criação de curso dependem de `areaId`, agora preenchido automaticamente quando `03 - Areas` roda antes. Publicar curso, playback e progresso podem depender de um conjunto coerente de area, curso, módulo, aula, vídeo e grants.

## Variáveis automáticas

Os scripts preenchem:

| Variável | Origem |
|---|---|
| `accessToken` | Login Admin e Refresh Token |
| `studentAccessToken` | Login Student |
| `userId` | Login Admin ou Student |
| `targetUserId` | Create User ou primeiro item de List Users |
| `areaId` | Create Area, ou primeiro item de List Areas |
| `courseId`, `courseSlug` | Create Course, List Available Courses ou Get Course Details |
| `moduleId`, `lessonId` | primeiro módulo/aula de Get Course Details |
| `videoId` | Create Video |
| `progressId` | Register Lesson Progress ou Get Course Progress |
| `uniqueEmail` | pre-request de Create/Update User e de Register |
| `uniqueAreaSlug` | pre-request de Create/Update Area |
| `uniqueCourseSlug` | pre-request de Create/Update Course |
| `correlationId` | pre-request global, renovado em cada request |

`accessToken` e `userId` também são preenchidos por `Register`, do mesmo jeito que `Login Admin`/`Login Student` fazem.

`Logout` remove os dois access tokens. URLs temporárias de playback e refresh tokens nunca são persistidos.

## Inventário dos endpoints

Todos os endpoints de controller exigem JSON nos bodies indicados. Erros de aplicação usam `ApiErrorResponse`; endpoints protegidos também podem retornar 401 e, quando há policy/permissão, 403.

| Módulo | Método e rota | Auth / policy | Entrada | Resposta principal | Status principais | Postman |
|---|---|---|---|---|---|---|
| Health | `GET /health` | Público | — | health agregado | 200, 503 | Health |
| Health | `GET /health/live` | Público | — | health mínimo | 200 | Live |
| Health | `GET /health/ready` | Público | — | health/readiness | 200, 503 | Ready |
| Auth | `POST /api/auth/login` | Público | body: `email`, `password` | `AuthResponse` | 200, 400, 401, 429, 500 | Login Admin/Student |
| Auth | `POST /api/auth/register` | Público | body: `name`, `email`, `password`, `captchaToken` | `AuthResponse` | 201, 400, 409, 429, 500 | Register |
| Auth | `POST /api/auth/confirm-email` | Bearer | body `token` | sem conteúdo | 204, 400, 401, 500 | Confirm Email |
| Auth | `POST /api/auth/resend-confirmation` | Bearer | — | sem conteúdo | 204, 401, 404, 409, 429, 500 | Resend Confirmation |
| Auth | `POST /api/auth/refresh-token` | Público | cookie HttpOnly; body fallback `refreshToken` | `AuthResponse` | 200, 400, 401, 429, 500 | Refresh Token |
| Auth | `POST /api/auth/logout` | Público | cookie HttpOnly; body fallback `refreshToken` | sem conteúdo | 204, 429, 500 | Logout |
| Users | `POST /api/users` | Bearer; `ManageUsers` | body: `name`, `email`, `password` | `UserResponse` | 201, 400, 401, 403, 409, 500 | Create User |
| Users | `PUT /api/users/{userId}` | Bearer; `ManageUsers` | path `userId`; body: `name`, `email`, `active` | `UserResponse` | 200, 400, 401, 403, 404, 409, 500 | Update User |
| Users | `GET /api/users` | Bearer; `ManageUsers` | query `page`, `pageSize` | `PagedResponse<UserResponse>` | 200, 400, 401, 403, 500 | List Users - Paged |
| Access | `POST /api/access/user-area` | Bearer; `ManageUserAreaAccess` | body: user/area/grant fields | `AreaAccessResponse` | 200, 400, 401, 403, 404, 500 | Grant User Area Access |
| Access | `POST /api/access/role-area` | Bearer; `ManageRoleAreaAccess` | body: role/area/grant fields | `AreaAccessResponse` | 200, 400, 401, 403, 404, 500 | Grant Role Area Access |
| Access | `POST /api/access/course/check` | Bearer; `CheckOwnCourseAccess`; deprecated | body `courseId` | `CourseAccessResponse` | 200, 400, 401, 403, 500 | Deprecated folder |
| Access | `GET /api/access/courses/{courseId}` | Bearer; `CheckOwnCourseAccess` | path `courseId` | `CourseAccessResponse` | 200, 400, 401, 500 | Check Own Course Access |
| Access | `GET /api/access/users/{userId}/courses/{courseId}` | Bearer; `CheckUserCourseAccess` | paths `userId`, `courseId` | `CourseAccessResponse` | 200, 400, 401, 403, 500 | Check User Course Access - Admin |
| Areas | `POST /api/areas` | Bearer; `ManageAreas` | body: `name`, `slug`, `description`, `displayOrder` | `AreaResponse` | 201, 400, 401, 403, 409, 500 | Create Area |
| Areas | `PUT /api/areas/{areaId}` | Bearer; `ManageAreas` | path `areaId`; body: `name`, `slug`, `description`, `displayOrder`, `active` | `AreaResponse` | 200, 400, 401, 403, 404, 409, 500 | Update Area |
| Areas | `GET /api/areas/{areaId}` | Bearer; `ManageAreas` | path `areaId` | `AreaResponse` | 200, 401, 403, 404, 500 | Get Area By Id |
| Areas | `GET /api/areas` | Bearer; `ManageAreas` | query opcional `active` | array de `AreaResponse` | 200, 400, 401, 403, 500 | List Areas |
| Courses | `POST /api/courses` | Bearer; `ManageCourses` | `CreateCourseRequest` | `CourseResponse` | 201, 400, 401, 403, 404, 409, 500 | Create Course |
| Courses | `PUT /api/courses/{courseId}` | Bearer; `ManageCourses` | path `courseId`; `UpdateCourseRequest` | `CourseResponse` | 200, 400, 401, 403, 404, 409, 500 | Update Course |
| Courses | `POST /api/courses/{courseId}/publish` | Bearer; `ManageCourses` | path `courseId` | `CourseResponse` | 200, 400, 401, 403, 404, 500 | Publish Course |
| Courses | `GET /api/courses/{courseId}` | Bearer | path `courseId` | `CourseDetailsResponse` | 200, 400, 401, 403, 404, 500 | Get Course Details |
| Courses | `GET /api/courses/available` | Bearer | query opcional `hasAccess` (`true`/`false`) | `CourseCatalogResponse` (`areas` + `courses`, cada curso com `hasAccess`) | 200, 400, 401, 403, 500 | List Available Courses |
| Videos | `POST /api/videos` | Bearer; `ManageVideos` | `CreateVideoRequest` | `VideoResponse` | 201, 400, 401, 403, 404, 409, 500 | Create Video |
| Videos | `POST /api/videos/{id}/ready` | Bearer; `ManageVideos` | path `id` | `VideoResponse` | 200, 400, 401, 403, 404, 409, 500 | Mark Video Ready |
| Videos | `GET /api/videos/{videoId}/playback` | Bearer | path `videoId` | `VideoPlaybackResponse` | 200, 400, 401, 403, 404, 409, 500 | Get Playback Url |
| Progress | `POST /api/progress/lessons` | Bearer | body `lessonId`, `watchedSeconds`; `markAsCompleted` é legado | `LessonProgressResponse` | 200, 400, 401, 403, 404, 500 | Register Lesson Progress |
| Progress | `GET /api/progress/courses/{courseId}` | Bearer | path `courseId` | `CourseProgressResponse` | 200, 400, 401, 403, 404, 500 | Get Course Progress |

Não há endpoint `me/current user`, controller de Audit Logs nem endpoints públicos de CRUD para roles, módulos ou aulas nesta versão. Areas têm CRUD administrativo completo desde `03 - Areas`; não há remoção física, apenas desativação via `PUT`. O módulo Audit Logs registra eventos internamente, mas não expõe listagem HTTP. Não foram inventadas requests para rotas inexistentes.

`GET /api/videos/{videoId}/playback` e `GET /api/progress/courses/{courseId}` eram `POST` com o id no corpo até esta versão; migraram para `GET` com o id na rota (leitura pura, sem efeito colateral) e não existem mais como `POST` — não há entrada de compatibilidade em `90 - Deprecated` para eles, diferente do tratamento dado a `access/course/check`. Ambos respondem com `Cache-Control: no-store`, já que carregam dado privado por usuário ou uma URL assinada com expiração.

`Create Course`/`Update Course` ganharam o campo `pricingModel` (`"Free"` ou `"Paid"`, curso nasce `Paid` se omitido). Um curso `Free` publicado é acessível a qualquer usuário autenticado com e-mail confirmado, mesmo sem grant de Area — inclusive sem Area nenhuma vinculada. `GET /api/courses/available` sempre mostra todas as Areas ativas e todos os cursos publicados; `hasAccess` reflete grant **ou** gratuidade, mas exige e-mail confirmado nos dois casos — recém-registrado sem confirmar vê o catálogo inteiro, só que tudo bloqueado.

As policies resolvem assim:

- `ManageUsers`: `users.manage` ou Admin;
- `ManageUserAreaAccess`: `users.manage` ou Admin;
- `ManageRoleAreaAccess`: `roles.manage` ou Admin;
- `ManageAreas`: `areas.manage` ou Admin;
- `CheckOwnCourseAccess`: usuário autenticado;
- `CheckUserCourseAccess`: `users.manage`, `areas.manage`, `courses.manage` ou Admin;
- `ManageCourses`: `courses.manage` ou Admin;
- `ManageVideos`: `videos.manage` ou Admin.

`AdminOnly` e `ReadProgress` existem na configuração, mas nenhum endpoint atual os referencia.

## Endpoint deprecated

`POST /api/access/course/check` está marcado `[Obsolete]`. Ele consulta somente o usuário autenticado e foi movido para `90 - Deprecated`. Use `GET /api/access/courses/{courseId}`.

O campo de request `markAsCompleted` de progresso e o campo `playbackUrl` de criação de vídeo ainda existem nos contratos por compatibilidade, mas a collection não depende deles. Eles não são endpoints deprecated.

## Cenários negativos

`95 - Negative Scenarios` contém:

- login com credenciais inválidas: 401;
- criação de usuário com senha fraca: 400;
- paginação de usuários inválida: 400;
- criação de curso com payload inválido: 400;
- consulta administrativa com token de aluno: 403;
- criação de vídeo com `storageKey` inválida: 400;
- progresso com `watchedSeconds` negativo: 400;
- criação de area com slug duplicado: 409.

Execute-os individualmente. O cenário 403 requer `Login Student` e um aluno sem as permissões administrativas. Vídeo/progresso exigem IDs sintaticamente válidos; para evitar que 404 esconda a validação pretendida, prefira IDs obtidos no fluxo positivo. O cenário de slug duplicado reutiliza `uniqueAreaSlug`, deixado preenchido por `03 - Areas` (`Create Area` ou `Update Area`) na mesma execução.

## Dependências e diagnósticos

`/openapi/v1.json` e `/scalar` só existem em `Development`; os testes aceitam 404 quando estão ocultos. `/health/ready` e `/health` podem retornar 503 enquanto o banco ou schema não estiver pronto.

`Create Course` depende de `areaId`, obtido de `03 - Areas`. `Get Course Details` depende de acesso ao curso. `Create Video` depende de `lessonId`; playback depende de vídeo pronto e acesso ao curso. Progress depende de aula/vídeo elegíveis. Grants dependem de users/roles/areas existentes; areas já podem ser criadas pela própria collection, roles ainda não.

Cada request envia um novo `X-Correlation-ID`; o teste global confirma o header na resposta. A collection valida `baseUrl` no pre-request e falha cedo se nenhum environment adequado estiver selecionado.

`Register` depende do Turnstile estar configurado no servidor (`Turnstile:SecretKey`); em `Development`/`Staging` sem chave, o servidor pula a verificação e aceita qualquer `captchaToken`. `Confirm Email`/`Resend Confirmation` dependem do Resend (`Resend:ApiKey`); sem chave fora de `Production`, o e-mail não é enviado de verdade — o servidor só loga um aviso, então pegue o token no log da aplicação em vez da caixa de entrada.
