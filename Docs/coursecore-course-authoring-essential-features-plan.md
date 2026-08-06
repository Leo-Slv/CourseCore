# CourseCore — Plano das funcionalidades essenciais de cadastro de cursos

## 1. Objetivo

Completar o fluxo administrativo de autoria de cursos com videoaulas. A API atual consegue criar um curso com módulos e aulas aninhados, cadastrar um vídeo, publicar o curso e oferecer playback, mas não permite manter essa estrutura adequadamente depois da criação inicial.

Este plano cobre:

- administração e ciclo de vida de cursos;
- CRUD lógico e ordenação de módulos;
- CRUD lógico e ordenação de aulas;
- upload e manutenção de vídeos;
- validação de prontidão antes da publicação;
- contratos HTTP, authorization, audit logs, testes e Postman.

O plano não implementa código. Cada bloco deve ser entregue incrementalmente, preservando Clean Architecture/DDD modular e os contratos já publicados quando for necessária compatibilidade.

## 2. Diagnóstico atual

### 2.1 Capacidades existentes

A API já oferece:

```text
POST /api/courses
PUT  /api/courses/{courseId}
POST /api/courses/{courseId}/publish
GET  /api/courses/{courseId}
GET  /api/courses/available

POST /api/videos
POST /api/videos/{videoId}/ready
POST /api/videos/playback
```

O domínio já possui:

- `Course.AddModule` e `Course.RemoveModule`;
- `Course.Publish` e `Course.Unpublish`;
- `CourseModule.AddLesson`, `RemoveLesson`, `Publish` e `Unpublish`;
- `Lesson.Publish`, `Unpublish`, `MarkAsFreePreview` e `RemoveFreePreview`;
- `Video` com mudança de metadata/storage e estados Processing, Ready e Failed;
- repositories de curso, aula e vídeo;
- `IVideoStorageService.GetUploadUrlAsync`, ainda não exposto por use case/controller.

### 2.2 Lacunas principais

- módulos e aulas só são criados dentro do payload inicial do curso;
- `CourseMapper.ApplyChanges` preserva deliberadamente a estrutura e não persiste edições de módulos/aulas;
- não há listagem administrativa paginada de todos os cursos;
- não há despublicação ou arquivamento;
- não há endpoint para solicitar upload URL;
- não há consulta/edição/substituição de vídeo;
- a publicação não valida se módulos, aulas e vídeos estão prontos;
- não há endpoints de reordenação;
- exclusões físicas podem colidir com progresso, audit logs e histórico.

## 3. Princípios de implementação

1. Controllers chamam use cases e presenters; não acessam DbContext ou repositories EF.
2. Escritas são executadas via `IUnitOfWork`.
3. Alterações usam métodos das entidades de domínio.
4. Operações destrutivas preferem arquivamento/despublicação ou remoção lógica.
5. IDs de curso/módulo/aula/vídeo devem ter ownership validado.
6. Nenhum cliente pode anexar uma aula a módulo de outro curso por manipulação de IDs.
7. Playback continua protegido pelo `CourseAccessService`.
8. Storage keys e URLs temporárias não devem aparecer em logs.
9. Endpoints existentes só mudam com estratégia explícita de compatibilidade.
10. Migrations são criadas apenas quando uma decisão de schema exigir novos campos.

## 4. Escopo de endpoints

### 4.1 Administração de cursos

| Método | Rota | Objetivo | Policy |
|---|---|---|---|
| `GET` | `/api/admin/courses` | Listagem administrativa paginada | `ManageCourses` |
| `GET` | `/api/admin/courses/{courseId}` | Detalhes administrativos sem exigir acesso de aluno | `ManageCourses` |
| `POST` | `/api/courses/{courseId}/unpublish` | Despublicar curso | `ManageCourses` |
| `POST` | `/api/courses/{courseId}/archive` | Arquivar curso | `ManageCourses` |
| `POST` | `/api/courses/{courseId}/restore` | Restaurar curso arquivado | `ManageCourses` |

Manter os endpoints atuais de criação, atualização, publicação e leitura do aluno.

### 4.2 Módulos

| Método | Rota | Objetivo |
|---|---|---|
| `POST` | `/api/courses/{courseId}/modules` | Criar módulo |
| `GET` | `/api/courses/{courseId}/modules` | Listar módulos administrativos |
| `GET` | `/api/modules/{moduleId}` | Consultar módulo |
| `PUT` | `/api/modules/{moduleId}` | Atualizar módulo |
| `POST` | `/api/modules/{moduleId}/publish` | Publicar módulo |
| `POST` | `/api/modules/{moduleId}/unpublish` | Despublicar módulo |
| `PATCH` | `/api/courses/{courseId}/modules/order` | Reordenar módulos atomicamente |
| `DELETE` | `/api/modules/{moduleId}` | Remover módulo quando permitido |

Todos exigem `ManageCourses`.

### 4.3 Aulas

| Método | Rota | Objetivo |
|---|---|---|
| `POST` | `/api/modules/{moduleId}/lessons` | Criar aula |
| `GET` | `/api/modules/{moduleId}/lessons` | Listar aulas administrativas |
| `GET` | `/api/lessons/{lessonId}` | Consultar aula |
| `PUT` | `/api/lessons/{lessonId}` | Atualizar aula |
| `POST` | `/api/lessons/{lessonId}/publish` | Publicar aula |
| `POST` | `/api/lessons/{lessonId}/unpublish` | Despublicar aula |
| `PATCH` | `/api/modules/{moduleId}/lessons/order` | Reordenar aulas atomicamente |
| `DELETE` | `/api/lessons/{lessonId}` | Remover aula quando permitido |

Todos exigem `ManageCourses`.

### 4.4 Vídeos e upload

| Método | Rota | Objetivo | Policy |
|---|---|---|---|
| `POST` | `/api/videos/upload-url` | Solicitar destino/URL de upload | `ManageVideos` |
| `GET` | `/api/videos/{videoId}` | Consultar metadata administrativa | `ManageVideos` |
| `PUT` | `/api/videos/{videoId}` | Atualizar metadata | `ManageVideos` |
| `POST` | `/api/videos/{videoId}/processing` | Marcar processamento/reprocessamento | `ManageVideos` |
| `POST` | `/api/videos/{videoId}/failed` | Marcar falha controlada | `ManageVideos` |
| `POST` | `/api/videos/{videoId}/replace` | Substituir referência de storage | `ManageVideos` |
| `DELETE` | `/api/videos/{videoId}` | Remover quando permitido | `ManageVideos` |

Manter criação, ready e playback atuais. O endpoint de playback continua sendo operação do aluno e não deve exigir `ManageVideos`.

## 5. Bloco A — Administração e ciclo de vida do curso

### 5.1 Listagem administrativa

Contrato recomendado:

```http
GET /api/admin/courses?page=1&pageSize=20&search=aspnet&published=false&archived=false
```

Resposta:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 0,
  "totalPages": 0
}
```

Regras:

- paginação obrigatória com limites compartilhados;
- busca case-insensitive por título e slug;
- filtros opcionais por publicação, arquivamento e área;
- ordenação estável por `displayOrder`, título e ID;
- incluir contadores de módulos/aulas/vídeos apenas se calculados eficientemente;
- não aplicar `CourseAccessService`, pois é visão administrativa protegida por policy.

### 5.2 Detalhes administrativos

O endpoint atual `GET /api/courses/{courseId}` representa a visão do aluno e valida acesso. Não reutilizá-lo para administração.

`GET /api/admin/courses/{courseId}` deve retornar:

- dados do curso;
- áreas;
- módulos e aulas, inclusive não publicados;
- resumo do vídeo de cada aula;
- readiness issues para publicação;
- estado arquivado, se implementado.

### 5.3 Despublicação

Criar `UnpublishCourseUseCase` usando `Course.Unpublish`.

Regras:

- idempotente: despublicar curso já não publicado retorna `200` com estado atual;
- não apagar progresso;
- impedir novas listagens/playbacks que dependam de curso publicado;
- registrar `CourseUnpublished`.

### 5.4 Arquivamento

Recomendação: adicionar `Archived` e `ArchivedAt` ao curso em uma migration específica, pois publicação e arquivamento representam estados diferentes.

Regras:

- curso arquivado não aparece no catálogo;
- não pode ser publicado enquanto arquivado;
- metadata e histórico permanecem disponíveis para administradores;
- progresso existente não é removido;
- restore não publica automaticamente;
- registrar `CourseArchived` e `CourseRestored`.

Se arquivamento for adiado, não implementar `DELETE /api/courses`; manter apenas despublicação.

## 6. Bloco B — Módulos

### 6.1 Contratos

`CreateCourseModuleRequest`:

```json
{
  "title": "Introdução",
  "description": "Fundamentos do curso",
  "displayOrder": 0
}
```

`UpdateCourseModuleRequest`:

```json
{
  "title": "Introdução atualizada",
  "description": "Fundamentos",
  "displayOrder": 0
}
```

`ReorderCourseModulesRequest`:

```json
{
  "items": [
    { "moduleId": "guid-1", "displayOrder": 0 },
    { "moduleId": "guid-2", "displayOrder": 1 }
  ]
}
```

### 6.2 Use cases

Criar:

```text
CreateCourseModuleUseCase
GetCourseModuleUseCase
ListCourseModulesUseCase
UpdateCourseModuleUseCase
PublishCourseModuleUseCase
UnpublishCourseModuleUseCase
ReorderCourseModulesUseCase
RemoveCourseModuleUseCase
```

### 6.3 Regras

- curso deve existir;
- módulo deve pertencer ao courseId informado;
- título/descrição usam limites existentes de Course;
- `displayOrder` não pode ser negativo;
- reordenação valida todos os IDs antes de persistir qualquer alteração;
- não aceitar IDs duplicados no payload;
- reordenação acontece em uma transação;
- módulo só pode ser publicado com pelo menos uma aula elegível;
- despublicar módulo não remove aulas ou progresso;
- remover módulo publicado deve exigir despublicação prévia;
- impedir remoção física quando houver progresso ou outras referências, salvo regra explícita;
- preferir `409 Conflict` para remoção bloqueada.

### 6.4 Persistência

O repository atual de curso não persiste mutações estruturais em `CourseMapper.ApplyChanges`. Antes dos use cases, decidir uma estratégia:

1. adicionar `ICourseModuleRepository` com operações específicas; ou
2. evoluir `ICourseRepository` para persistir o aggregate completo com diff seguro.

Recomendação: criar `ICourseModuleRepository`, pois evita limpar/recriar coleções rastreadas e reduz risco sobre aulas, vídeos e progresso.

Operações mínimas:

```text
FindByIdAsync
ListByCourseIdAsync
CreateAsync
UpdateAsync
RemoveAsync
```

## 7. Bloco C — Aulas

### 7.1 Contratos

`CreateLessonRequest`:

```json
{
  "title": "Primeira aula",
  "description": "Apresentação",
  "displayOrder": 0,
  "freePreview": false
}
```

`UpdateLessonRequest`:

```json
{
  "title": "Primeira aula atualizada",
  "description": "Apresentação",
  "displayOrder": 0,
  "freePreview": true
}
```

Reordenação segue o mesmo formato de módulos, usando `lessonId`.

### 7.2 Use cases

Criar:

```text
CreateLessonUseCase
GetLessonUseCase
ListLessonsUseCase
UpdateLessonUseCase
PublishLessonUseCase
UnpublishLessonUseCase
ReorderLessonsUseCase
RemoveLessonUseCase
```

### 7.3 Regras

- módulo deve existir;
- aula deve pertencer ao módulo esperado;
- `freePreview` só muda por método de domínio;
- aula só pode ser publicada quando possuir vídeo Ready, salvo decisão explícita para aula sem vídeo;
- despublicação não apaga progresso;
- remoção de aula com vídeo ou progresso deve ser bloqueada ou convertida em arquivamento;
- mover aula entre módulos fica fora da primeira entrega; se necessário, criar use case dedicado com validação de mesmo curso;
- reordenação deve ser atômica e não aceitar IDs de outro módulo.

### 7.4 Repository

O `ILessonRepository` atual é somente leitura. Evoluir com:

```text
CreateAsync
UpdateAsync
RemoveAsync
ExistsProgressAsync ou consulta equivalente
```

Alternativamente, criar repository de módulo que gerencie suas aulas como aggregate, desde que o diff de persistência seja seguro e testado.

## 8. Bloco D — Upload e manutenção de vídeo

### 8.1 Upload URL

O contrato `IVideoStorageService.GetUploadUrlAsync` já existe, mas não recebe provider, content type ou tamanho. Antes de expor o endpoint, definir contrato extensível:

```json
{
  "lessonId": "guid",
  "fileName": "lesson-01.mp4",
  "contentType": "video/mp4",
  "sizeBytes": 104857600,
  "storageProvider": "Local"
}
```

Resposta sugerida:

```json
{
  "storageProvider": "Local",
  "storageKey": "courses/.../lesson-01.mp4",
  "uploadUrl": "temporary-url",
  "expiresAt": "2026-08-05T22:00:00Z",
  "headers": {}
}
```

Regras:

- validar que lessonId existe;
- gerar storage key no servidor; não confiar em caminho arbitrário do cliente;
- validar provider permitido, extensão, MIME type e tamanho;
- upload URL curta e temporária;
- não persistir upload URL;
- não registrar URL, signature ou headers sensíveis;
- rate limiting moderado para geração de URLs;
- Local, S3, Azure/R2 etc. devem implementar o mesmo contrato sem condicional no controller.

### 8.2 Metadata e lifecycle

Criar use cases:

```text
GetVideoUseCase
UpdateVideoUseCase
RequestVideoUploadUseCase
ReplaceVideoStorageUseCase
MarkVideoProcessingUseCase
MarkVideoFailedUseCase
RemoveVideoUseCase
```

Regras:

- no máximo um vídeo ativo por aula, conforme constraint/modelo atual;
- substituir storage retorna status Processing;
- ready continua sendo transição controlada;
- falha não expõe detalhes internos do provider ao aluno;
- update de metadata não altera storage inadvertidamente;
- remoção de vídeo de aula publicada deve ser bloqueada ou despublicar a aula de forma explícita;
- playback só funciona para vídeo Ready;
- URL assinada continua gerada apenas sob demanda.

### 8.3 Remoção de arquivo

O contrato atual não possui delete no storage. Antes de implementar `DELETE /api/videos/{videoId}`, adicionar operação explícita ao storage service ou adotar cleanup assíncrono/outbox.

Não excluir registro e deixar arquivo órfão silenciosamente. Não excluir arquivo antes do commit do banco sem estratégia de compensação.

## 9. Bloco E — Readiness para publicação

### 9.1 Problema atual

`PublishCourseUseCase` chama `Course.Publish()` sem validar a estrutura. Um curso incompleto pode ser publicado.

### 9.2 Regras mínimas

Um curso só pode ser publicado quando:

- não está arquivado;
- possui ao menos uma área ativa;
- possui ao menos um módulo;
- todos os módulos que compõem a experiência estão publicados ou prontos para publicação;
- cada módulo possui ao menos uma aula;
- aulas obrigatórias estão publicadas ou prontas;
- toda aula publicada possui vídeo Ready;
- título, slug e descrição atendem aos limites;
- não há ordens inválidas ou IDs relacionados a outro aggregate.

Decidir explicitamente se módulos/aulas são publicados automaticamente junto com o curso ou devem estar publicados antes. Recomendação: publicação explícita de cada nível, com endpoint de readiness que explique pendências.

### 9.3 Endpoint de readiness

```text
GET /api/admin/courses/{courseId}/readiness
```

Resposta sugerida:

```json
{
  "ready": false,
  "issues": [
    {
      "code": "lesson.video_not_ready",
      "entityId": "lesson-guid",
      "message": "Lesson requires a ready video."
    }
  ]
}
```

Não retornar stack trace, storage key ou configuração do provider.

### 9.4 Application service

Criar `CourseReadinessService` na Application, dependendo de contracts/repositories, e reutilizá-lo em:

- `GetCourseReadinessUseCase`;
- `PublishCourseUseCase`;
- testes de integração.

Evitar duplicar a regra no controller ou no frontend.

## 10. Responses e presenters

Criar respostas administrativas distintas das respostas do aluno:

```text
AdminCourseListItemResponse
AdminCourseDetailsResponse
CourseReadinessResponse
CourseModuleResponse
LessonResponse
VideoAdminResponse
VideoUploadResponse
```

`VideoAdminResponse` pode incluir provider e storage key somente para usuários com `ManageVideos`; respostas de aluno não devem expor storage key.

Presenters devem mapear outputs, não entidades EF.

## 11. Authorization

Reutilizar:

```text
ManageCourses -> courses.manage ou Admin
ManageVideos  -> videos.manage ou Admin
```

Regras:

- administração de curso/módulo/aula usa `ManageCourses`;
- upload e lifecycle de vídeo usam `ManageVideos`;
- quando uma operação combina aula e vídeo, exigir a policy da ação principal e validar ownership no use case;
- playback permanece apenas autenticado, condicionado ao acesso ao curso;
- não aceitar permission ou userId vindo do body para decidir autorização.

## 12. Audit logs

Adicionar action names:

```text
CourseUnpublished
CourseArchived
CourseRestored
CourseModuleCreated
CourseModuleUpdated
CourseModulePublished
CourseModuleUnpublished
CourseModuleRemoved
CourseModulesReordered
LessonCreated
LessonUpdated
LessonPublished
LessonUnpublished
LessonRemoved
LessonsReordered
VideoUploadRequested
VideoUpdated
VideoProcessing
VideoFailed
VideoReplaced
VideoRemoved
```

Metadata deve usar IDs, action e changed fields. Não registrar URLs assinadas, storage credentials, tokens ou payload completo.

## 13. Concorrência e consistência

Reordenação e edição simultânea podem causar lost updates. Avaliar:

- concurrency token/row version nos modelos editáveis; ou
- update condicional por `UpdatedAt`; ou
- transação com validação final.

Requisitos mínimos:

- reorder é transacional;
- todos os IDs são validados antes do primeiro update;
- falha intermediária não deixa ordem parcial;
- slugs continuam únicos;
- substituição de vídeo não produz dois registros ativos para a mesma aula;
- publicação usa snapshot consistente da estrutura.

Uma migration de concurrency token deve ser proposta separadamente e justificada.

## 14. Estratégia de remoção

Antes de implementar DELETE, definir regras para dependências:

| Recurso | Dependências | Estratégia inicial recomendada |
|---|---|---|
| Curso | módulos, aulas, vídeos, progresso, áreas | arquivar, não apagar |
| Módulo | aulas, vídeos, progresso indireto | bloquear se não estiver vazio |
| Aula | vídeo e progresso | bloquear se houver dependências |
| Vídeo | storage e progresso de aula | bloquear em aula publicada; cleanup controlado |

Evitar cascade delete sobre conteúdo acadêmico e progresso sem decisão explícita de produto e retenção.

## 15. Etapas de implementação

### Etapa 1 — Read models administrativos

- listagem administrativa paginada;
- detalhes administrativos;
- consultas de módulo, aula e vídeo;
- readiness somente leitura;
- testes e Postman.

### Etapa 2 — CRUD de módulos

- repository específico;
- create/update/publish/unpublish/reorder;
- remoção bloqueada quando houver dependências;
- audit logs e testes.

### Etapa 3 — CRUD de aulas

- evolução do `ILessonRepository`;
- create/update/free preview/publish/unpublish/reorder;
- remoção segura;
- audit logs e testes.

### Etapa 4 — Upload e manutenção de vídeo

- contrato de upload provider-agnostic;
- geração server-side de storage key;
- metadata update e estados;
- substituição e cleanup;
- testes de segurança e integração.

### Etapa 5 — Publicação consistente

- `CourseReadinessService`;
- bloquear publicação incompleta com `409 Conflict` ou `400` padronizado;
- endpoint de readiness;
- despublicação;
- audit logs.

### Etapa 6 — Arquivamento

- decisão de schema;
- migration isolada, se aprovada;
- archive/restore;
- filtros administrativos;
- testes de retenção.

## 16. Testes obrigatórios

### 16.1 Unit/Application

- ownership entre curso, módulo e aula;
- criação/edição válida e inválida;
- limites de strings e coleções;
- reordenação atômica;
- IDs duplicados ou externos;
- publish/unpublish idempotente;
- readiness para cada pendência;
- upload com MIME/provider/tamanho inválidos;
- transições válidas e inválidas de vídeo;
- conflitos de remoção;
- audit logs;
- Unit of Work em todas as escritas.

### 16.2 Integração HTTP

- autenticação 401 e policies 403;
- criação e manutenção completa de módulo/aula;
- listagem administrativa paginada;
- curso do aluno continua validando acesso;
- publicação incompleta rejeitada;
- publicação completa aceita;
- reordenação persiste ordem correta;
- operações cruzando aggregates retornam 404/409 sem alterar dados;
- vídeo Processing não oferece playback;
- vídeo Ready oferece playback para usuário autorizado;
- erros retornam `ApiErrorResponse`, trace ID e correlation ID;
- OpenAPI marca policies corretamente.

### 16.3 Regressão

- criação aninhada de curso continua funcionando;
- grants e resolução de áreas não sofrem regressão;
- progresso existente não é apagado por despublicação;
- refresh/login e demais módulos continuam passando;
- Postman existente continua importável.

Os testes HTTP devem usar a infraestrutura SQLite in-memory existente; não executar seed real, PostgreSQL externo ou database update durante a suíte.

## 17. Postman

Reorganizar `04 - Courses` e `05 - Media / Videos` com subpastas:

```text
04 - Courses
  Admin
  Modules
  Lessons
  Student

05 - Media / Videos
  Upload
  Administration
  Playback
```

Automatizar:

- `courseId` a partir de create/admin list;
- `moduleId` a partir de create/list;
- `lessonId` a partir de create/list;
- `videoId` a partir de create/get;
- slug e nomes únicos;
- upload URL nunca persistida como variável permanente;
- scripts seguros quando listas estiverem vazias.

Adicionar cenários negativos para ownership, publish readiness, reorder inválido, provider/storage inválido e playback não autorizado.

## 18. Documentação a atualizar

```text
Docs/implementation-class-diagram.md
Docs/postman.md
README.md
Docs/coursecore-security-hardening-backlog.md
Postman/CourseCore.postman_collection.json
Postman/CourseCore.local.postman_environment.json
```

Documentar claramente:

- diferença entre visão administrativa e visão do aluno;
- lifecycle de curso/módulo/aula/vídeo;
- pré-requisitos de publicação;
- fluxo de upload por provider;
- política de remoção e retenção;
- campos/IDs preenchidos automaticamente no Postman.

## 19. Migrations

Não criar migration para CRUD de módulos/aulas se as tabelas atuais forem suficientes.

Migrations potencialmente necessárias, cada uma separada e previamente aprovada:

- `Archived`/`ArchivedAt` em cursos;
- concurrency token;
- soft delete/archived em módulos, aulas ou vídeos;
- metadata de upload/processamento que não exista atualmente.

Nunca combinar todas essas decisões em uma migration genérica. Cada mudança deve ter diagnóstico, rollback e testes de compatibilidade.

## 20. Validação de cada etapa

Executar:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet list package --vulnerable --include-transitive
docker compose config --quiet
```

Validar Postman:

```powershell
Get-Content Postman/CourseCore.postman_collection.json -Raw | ConvertFrom-Json | Out-Null
Get-Content Postman/CourseCore.local.postman_environment.json -Raw | ConvertFrom-Json | Out-Null
```

Revisar:

```powershell
git diff --check
git status
git status --ignored
git diff --stat
```

Confirmar:

- nenhuma URL assinada, token, cookie ou secret foi versionado;
- `.env`, `bin/`, `obj/` e artifacts continuam ignorados;
- nenhuma migration foi aplicada a banco real sem autorização;
- seed real não foi executado durante testes;
- nenhuma exclusão apagou progresso ou conteúdo sem regra explícita;
- todos os endpoints existentes mantiveram compatibilidade ou foram documentados como deprecated.

## 21. Fora de escopo

Não implementar neste plano:

- CRUD de Areas, coberto por plano separado;
- roles, permissions ou atribuição de usuários;
- certificados;
- avaliações, quizzes ou exercícios;
- comentários e fóruns;
- pagamentos e assinaturas;
- recomendações ou busca avançada;
- transcodificação real por provider específico sem contrato aprovado;
- DRM;
- analytics avançado de visualização;
- alteração do cálculo de progresso, salvo adaptação estritamente necessária ao lifecycle.

## 22. Critérios de aceite finais

- administradores conseguem listar e consultar todos os cursos;
- módulos podem ser criados, editados, ordenados, publicados e despublicados;
- aulas podem ser criadas, editadas, ordenadas, publicadas, despublicadas e configuradas como preview;
- remoções são bloqueadas ou tratadas sem apagar histórico indevidamente;
- upload de vídeo possui fluxo seguro e provider-agnostic;
- vídeos podem ser consultados, atualizados, substituídos e ter lifecycle controlado;
- curso incompleto não pode ser publicado;
- endpoint de readiness explica pendências de forma segura;
- despublicação preserva conteúdo e progresso;
- authorization usa `ManageCourses` e `ManageVideos` corretamente;
- ações administrativas geram audit logs sem dados sensíveis;
- Postman permite encadear courseId, moduleId, lessonId e videoId;
- OpenAPI e documentação refletem todos os novos contratos;
- build, testes e auditoria de pacotes passam;
- migrations só existem quando justificadas por mudança de schema.
