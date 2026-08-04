# Postman

A collection do CourseCore cobre os 24 endpoints executáveis da API: 21 actions de controllers e 3 health checks. Ela também inclui 2 requests de diagnóstico disponíveis apenas em `Development` e 7 cenários negativos manuais.

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
- `areaId` e `roleId`: não há endpoint público para criar ou listar esses recursos;
- `targetUserId`, `courseId`, `lessonId` e `videoId`: somente quando o recurso não puder ser obtido por um request anterior;
- `videoStorageKey`: chave válida no provider configurado.

Nunca salve credenciais reais, JWT, refresh token, cookie ou connection string nos arquivos versionados.

## Autenticação automática

A collection usa Bearer `{{accessToken}}` globalmente. Health, login, refresh, logout e diagnósticos sobrescrevem a autenticação com `No Auth`.

`Login Admin` envia `adminEmail`/`adminPassword`, lê o contrato real `token.accessToken`, salva o JWT em `accessToken` e salva `userId`. `Login Student` faz o mesmo em `studentAccessToken`, usado explicitamente pelo cenário negativo de autorização.

O refresh token não é salvo em variável. Login grava o refresh token em cookie `HttpOnly`; o cookie jar do Postman o envia automaticamente a `Refresh Token` e `Logout`. O body mantém `refreshToken` vazio apenas por compatibilidade com o contrato. `Refresh Token` salva o novo `token.accessToken` em `accessToken`. Após `200` ou `204`, `Logout` remove `accessToken` e `studentAccessToken` do environment; a API apaga o cookie.

Login, refresh e logout têm rate limiting. Não há loop agressivo de 429 na collection.

## Ordem sugerida no Collection Runner

As pastas numeradas expressam a ordem operacional:

1. `00 - Health`;
2. `01 - Auth` — execute `Login Admin`; `Login Student` é opcional;
3. `02 - Users`;
4. `03 - Access`;
5. `04 - Courses`;
6. `05 - Media / Videos`;
7. `06 - Progress`;
8. `90 - Deprecated` apenas quando necessário;
9. `95 - Negative Scenarios` individualmente, após preparar suas dependências;
10. `98 - Session Cleanup`;
11. `99 - Diagnostics` apenas em `Development`.

O Runner não cria areas ou roles, pois a API não expõe endpoints para isso. Fluxos de criação de curso dependem de `areaId` existente. Publicar curso, playback e progresso podem depender de um conjunto coerente de area, curso, módulo, aula, vídeo e grants.

## Variáveis automáticas

Os scripts preenchem:

| Variável | Origem |
|---|---|
| `accessToken` | Login Admin e Refresh Token |
| `studentAccessToken` | Login Student |
| `userId` | Login Admin ou Student |
| `targetUserId` | Create User ou primeiro item de List Users |
| `courseId`, `courseSlug` | Create Course, List Available Courses ou Get Course Details |
| `moduleId`, `lessonId` | primeiro módulo/aula de Get Course Details |
| `videoId` | Create Video |
| `progressId` | Register Lesson Progress ou Get Course Progress |
| `uniqueEmail` | pre-request de Create/Update User |
| `uniqueCourseSlug` | pre-request de Create/Update Course |
| `correlationId` | pre-request global, renovado em cada request |

`Logout` remove os dois access tokens. URLs temporárias de playback e refresh tokens nunca são persistidos.

## Inventário dos endpoints

Todos os endpoints de controller exigem JSON nos bodies indicados. Erros de aplicação usam `ApiErrorResponse`; endpoints protegidos também podem retornar 401 e, quando há policy/permissão, 403.

| Módulo | Método e rota | Auth / policy | Entrada | Resposta principal | Status principais | Postman |
|---|---|---|---|---|---|---|
| Health | `GET /health` | Público | — | health agregado | 200, 503 | Health |
| Health | `GET /health/live` | Público | — | health mínimo | 200 | Live |
| Health | `GET /health/ready` | Público | — | health/readiness | 200, 503 | Ready |
| Auth | `POST /api/auth/login` | Público | body: `email`, `password` | `AuthResponse` | 200, 400, 401, 429, 500 | Login Admin/Student |
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
| Courses | `POST /api/courses` | Bearer; `ManageCourses` | `CreateCourseRequest` | `CourseResponse` | 201, 400, 401, 403, 404, 409, 500 | Create Course |
| Courses | `PUT /api/courses/{courseId}` | Bearer; `ManageCourses` | path `courseId`; `UpdateCourseRequest` | `CourseResponse` | 200, 400, 401, 403, 404, 409, 500 | Update Course |
| Courses | `POST /api/courses/{courseId}/publish` | Bearer; `ManageCourses` | path `courseId` | `CourseResponse` | 200, 400, 401, 403, 404, 500 | Publish Course |
| Courses | `GET /api/courses/{courseId}` | Bearer | path `courseId` | `CourseDetailsResponse` | 200, 400, 401, 403, 404, 500 | Get Course Details |
| Courses | `GET /api/courses/available` | Bearer | — | array de `CourseListItemResponse` | 200, 400, 401, 403, 500 | List Available Courses |
| Videos | `POST /api/videos` | Bearer; `ManageVideos` | `CreateVideoRequest` | `VideoResponse` | 201, 400, 401, 403, 404, 409, 500 | Create Video |
| Videos | `POST /api/videos/{id}/ready` | Bearer; `ManageVideos` | path `id` | `VideoResponse` | 200, 400, 401, 403, 404, 409, 500 | Mark Video Ready |
| Videos | `POST /api/videos/playback` | Bearer | body `videoId` | `VideoPlaybackResponse` | 200, 400, 401, 403, 404, 409, 500 | Get Playback Url |
| Progress | `POST /api/progress/lessons` | Bearer | body `lessonId`, `watchedSeconds`; `markAsCompleted` é legado | `LessonProgressResponse` | 200, 400, 401, 403, 404, 500 | Register Lesson Progress |
| Progress | `POST /api/progress/courses` | Bearer | body `courseId` | `CourseProgressResponse` | 200, 400, 401, 403, 404, 500 | Get Course Progress |

Não há endpoint `me/current user`, controller de Audit Logs nem endpoints públicos de CRUD para areas, roles, módulos ou aulas nesta versão. O módulo Audit Logs registra eventos internamente, mas não expõe listagem HTTP. Não foram inventadas requests para rotas inexistentes.

As policies resolvem assim:

- `ManageUsers`: `users.manage` ou Admin;
- `ManageUserAreaAccess`: `users.manage` ou Admin;
- `ManageRoleAreaAccess`: `roles.manage` ou Admin;
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
- progresso com `watchedSeconds` negativo: 400.

Execute-os individualmente. O cenário 403 requer `Login Student` e um aluno sem as permissões administrativas. Vídeo/progresso exigem IDs sintaticamente válidos; para evitar que 404 esconda a validação pretendida, prefira IDs obtidos no fluxo positivo.

## Dependências e diagnósticos

`/openapi/v1.json` e `/scalar` só existem em `Development`; os testes aceitam 404 quando estão ocultos. `/health/ready` e `/health` podem retornar 503 enquanto o banco ou schema não estiver pronto.

`Create Course` depende de `areaId`. `Get Course Details` depende de acesso ao curso. `Create Video` depende de `lessonId`; playback depende de vídeo pronto e acesso ao curso. Progress depende de aula/vídeo elegíveis. Grants dependem de users/roles/areas existentes.

Cada request envia um novo `X-Correlation-ID`; o teste global confirma o header na resposta. A collection valida `baseUrl` no pre-request e falha cedo se nenhum environment adequado estiver selecionado.
