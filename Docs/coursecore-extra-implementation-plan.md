# CourseCore API — Planejamento de Etapas Extras com Codex

## 1. Objetivo do documento

Este documento formaliza as etapas extras recomendadas para o backend **CourseCore API** após a conclusão do plano principal de implementação.

O plano principal foi concluído nas 22 posições previstas. Portanto, as etapas deste documento **não substituem** o planejamento original e **não devem ser numeradas como posição 23, 24, 25 etc.**

A partir daqui, a nomenclatura correta será:

```text
Extra 01
Extra 02
Extra 03
...
```

Este arquivo deve ser usado como referência para orientar prompts futuros enviados ao Codex.

---

## 2. Contexto atual do projeto

O projeto já possui uma base funcional implementada, incluindo:

```text
- arquitetura Clean Architecture / DDD modular;
- separação entre Domain Entity e PersistenceModel;
- entidades de domínio;
- PersistenceModels;
- DbContext com EF Core/PostgreSQL;
- Fluent API configurations;
- mappers PersistenceModel <-> Domain;
- repositórios EF;
- UnitOfWork;
- Dependency Injection modular;
- autenticação JWT;
- BCryptPasswordHasher;
- refresh token persistido, hasheado, rotacionado e revogável;
- use cases principais;
- requests, responses e presenters;
- controllers modulares protegidos com Authorize/policies;
- CurrentUserService implementado via IHttpContextAccessor;
- userId sensível removido dos contratos HTTP de consumo;
- migration inicial;
- migration de refresh_tokens;
- PostgreSQL local validado;
- seed admin opt-in validado;
- middleware global de exceções;
- exceções específicas de Application;
- Scalar/OpenAPI configurado em Development;
- JWT Bearer documentado no OpenAPI/Scalar;
- vulnerabilidade Microsoft.OpenApi/NU1903 corrigida;
- health checks públicos para live/ready/general;
- testes unitários principais;
- testes de integração HTTP básicos com SQLite in-memory;
- status HTTP de criação padronizados com 201 Created/CreatedAtAction onde aplicável;
- lookup de curso por aula otimizado via repositório.
```

Pendências conhecidas após as etapas já executadas:

```text
- GET /api/courses/{courseId} ainda precisa validar acesso funcional do usuário ao curso;
- revisão final para readiness de produção ainda precisa ser feita;
- OpenAPI usa security requirement global, podendo marcar endpoints AllowAnonymous como protegidos;
- CORS, logs estruturados, secrets, HTTPS/proxy e deploy ainda precisam ser revisados para produção;
- testes de integração ainda são básicos e não cobrem todos os módulos;
- políticas ainda são baseadas principalmente em role Admin, sem granularidade real por permission claim.
```

---

## 3. Regras fixas para todas as etapas extras

Em todos os prompts enviados ao Codex para etapas extras, as seguintes regras devem ser respeitadas:

```text
1. Considerar somente o estado atual do repositório.
2. Ignorar tentativas antigas, conversas antigas ou suposições externas.
3. Não implementar fora do escopo da etapa extra atual.
4. Não misturar mais de uma etapa extra no mesmo commit.
5. Não alterar Domain Entities sem necessidade real e explícita.
6. Não mapear Domain Entities diretamente no DbContext.
7. Não fazer controller acessar DbContext diretamente.
8. Não fazer controller acessar repositório EF concreto.
9. Não colocar regra de negócio em controller.
10. Não alterar migrations existentes sem necessidade real.
11. Não executar database update quando a etapa não pedir isso.
12. Não rodar seed quando a etapa não pedir isso.
13. Não commitar senha, token, segredo JWT real ou connection string real.
14. Ao final de cada etapa, rodar dotnet build.
15. Só commitar se o build passar e o diff estiver dentro do escopo.
16. Usar Conventional Commits.
17. O commit deve descrever a mudança real, não o número da etapa.
18. Informar arquivos criados, alterados, removidos, warnings, erros e pendências.
19. Quando existirem testes, rodar dotnet test ao final da etapa.
20. Quando a etapa mexer em pacotes, rodar dotnet list package --vulnerable --include-transitive.
```

---

## 4. Ordem recomendada das etapas extras

A ordem abaixo reflete a sequência real adotada após o plano principal, corrigindo a numeração para manter o histórico coerente.

```text
Extra 01 - Configurar Scalar/OpenAPI documentation
Extra 02 - Implementar CurrentUserService, policies e proteção dos controllers
Extra 03 - Remover userId sensível dos requests e usar usuário autenticado
Extra 04 - Documentar JWT Bearer no Scalar/OpenAPI
Extra 05 - Resolver vulnerabilidade NU1903 / Microsoft.OpenApi
Extra 06 - Corrigir PostgreSQL local, aplicar migration e validar seed admin
Extra 07 - Melhorar refresh token com persistência real
Extra 08 - Criar testes automatizados principais
Extra 09 - Otimizar localização de curso por aula
Extra 10 - Padronizar status HTTP e CreatedAtAction
Extra 11 - Criar health checks e validações de runtime
Extra 12 - Criar testes de integração HTTP básicos
Extra 13 - Validar acesso do usuário em detalhes do curso
Extra 14 - Revisão final para ambiente de produção
```

---

## 4.1 Status das etapas extras

```text
Extra 01 - Concluída
Extra 02 - Concluída
Extra 03 - Concluída
Extra 04 - Concluída
Extra 05 - Concluída
Extra 06 - Concluída
Extra 07 - Concluída
Extra 08 - Concluída
Extra 09 - Concluída
Extra 10 - Concluída
Extra 11 - Concluída
Extra 12 - Concluída
Extra 13 - Próxima etapa recomendada
Extra 14 - Pendente
```

Commits já realizados nas etapas extras:

```text
3712748 docs: add scalar api documentation
2cf7b26 feat: add authorization policies and current user service
7de10bc fix: use authenticated user in sensitive endpoints
d155294 docs: document jwt bearer in openapi
cd3776b build: update vulnerable openapi dependency
bf95a3d feat: persist and rotate refresh tokens
eb2f3f6 test: add core domain and auth use case tests
918e231 refactor: optimize course lookup by lesson
65186b4 fix: standardize api response status codes
2fdeaa3 feat: add health check endpoints
4247362 test: add basic api integration tests
```

Observação: a Extra 06 foi uma etapa de validação runtime/local e não gerou commit, pois não houve alteração versionável.

---

# 5. Planejamento por etapa extra

---

## Extra 01 — Configurar Scalar/OpenAPI documentation

### Objetivo

Adicionar a interface Scalar para visualizar a documentação OpenAPI da API em ambiente de desenvolvimento.

### Escopo

Verificar e instalar, se necessário:

```text
Scalar.AspNetCore
```

Alterar:

```text
CourseCore.csproj
Program.cs
Properties/launchSettings.json, se existir e fizer sentido
```

### Regras

```text
- Manter builder.Services.AddOpenApi().
- Manter app.MapOpenApi() em Development.
- Adicionar app.MapScalarApiReference() em Development.
- Não configurar Scalar em Production.
- Não alterar controllers.
- Não alterar rotas.
- Não instalar Swagger/Swashbuckle.
```

### Entrega esperada

```text
- Scalar.AspNetCore instalado;
- /scalar disponível em Development;
- /openapi/v1.json preservado;
- build funcionando;
- commit: docs: add scalar api documentation.
```

---

## Extra 02 — Implementar CurrentUserService, policies e proteção dos controllers

### Objetivo

Implementar a base de autorização da API e proteger os controllers com autenticação/autorização.

### Escopo

Criar:

```text
Shared/Infrastructure/Security/CurrentUserService.cs
Modules/Auth/Application/Constants/AuthClaimTypes.cs
Modules/Auth/Application/Constants/AuthRoleNames.cs
Modules/Auth/Application/Constants/AuthPolicyNames.cs
```

Alterar:

```text
Shared/SharedDependencyInjection.cs
Modules/Auth/AuthDependencyInjection.cs
Modules/Auth/Presentation/Controllers/AuthController.cs
Modules/Users/Presentation/Controllers/UsersController.cs
Modules/Access/Presentation/Controllers/AreasController.cs
Modules/Courses/Presentation/Controllers/CoursesController.cs
Modules/Media/Presentation/Controllers/VideosController.cs
Modules/Progress/Presentation/Controllers/ProgressController.cs
```

### Regras

```text
- Implementar ICurrentUserService usando IHttpContextAccessor.
- Registrar IHttpContextAccessor.
- Registrar ICurrentUserService.
- Criar policy AdminOnly.
- Criar policies básicas: ManageUsers, ManageAccess, ManageCourses, ManageVideos, ReadProgress.
- Nesta etapa, policies podem exigir apenas role Admin.
- AuthController deve manter login e refresh como AllowAnonymous.
- Controllers administrativos devem exigir policy.
- Controllers de leitura do usuário devem exigir pelo menos Authorize.
```

### Não deve fazer

```text
- Não remover userId dos requests ainda.
- Não alterar use cases.
- Não alterar rotas.
- Não alterar presenters.
- Não alterar migrations.
- Não executar database update.
- Não rodar seed.
```

### Entrega esperada

```text
- CurrentUserService implementado;
- policies registradas;
- controllers protegidos;
- AuthController preservado como AllowAnonymous no login/refresh;
- build funcionando;
- commit: feat: add authorization policies and current user service.
```

---

## Extra 03 — Remover userId sensível dos requests e usar usuário autenticado

### Objetivo

Remover a possibilidade de o cliente informar `UserId` em fluxos sensíveis e passar a usar o usuário autenticado via `ICurrentUserService`.

### Escopo

Alterar requests, presenters e controllers relacionados a:

```text
GET /api/courses/available
POST /api/videos/playback
POST /api/progress/lessons
POST /api/progress/courses
POST /api/access/course/check, se aplicável
```

Possíveis arquivos:

```text
Modules/Courses/Presentation/Controllers/CoursesController.cs
Modules/Courses/Presentation/Requests ou inputs relacionados
Modules/Courses/Presentation/Presenters/CoursePresenter.cs

Modules/Media/Presentation/Controllers/VideosController.cs
Modules/Media/Presentation/Requests/RequestVideoPlaybackRequest.cs
Modules/Media/Presentation/Presenters/VideoPresenter.cs

Modules/Progress/Presentation/Controllers/ProgressController.cs
Modules/Progress/Presentation/Requests/*.cs
Modules/Progress/Presentation/Presenters/ProgressPresenter.cs

Modules/Access/Presentation/Controllers/AreasController.cs
Modules/Access/Presentation/Requests/CheckCourseAccessRequest.cs
Modules/Access/Presentation/Presenters/AccessPresenter.cs
```

### Regras

```text
- O UserId deve vir do token JWT via ICurrentUserService.
- Se usuário não estiver autenticado, retornar 401.
- Se UserId da claim for inválido, retornar 401 ou erro padronizado.
- Não aceitar userId do cliente em body/query nos fluxos sensíveis.
- Não alterar entidades de domínio.
- Não alterar banco.
```

### Entrega esperada

```text
- userId removido dos contratos HTTP sensíveis;
- controllers usam ICurrentUserService;
- endpoints continuam compilando;
- build funcionando;
- commit: fix: use authenticated user in sensitive endpoints.
```

---

## Extra 04 — Documentar JWT Bearer no Scalar/OpenAPI

### Objetivo

Configurar o OpenAPI para documentar autenticação JWT Bearer na interface Scalar.

### Escopo

Alterar:

```text
Program.cs
```

Opcionalmente criar extensões em:

```text
Shared/Presentation/OpenApi/
```

### Regras

```text
- Documentar security scheme Bearer.
- Permitir uso do token JWT na interface Scalar.
- Não alterar lógica de autenticação.
- Não alterar controllers, salvo se for apenas atributo de documentação.
- Não alterar rotas.
- Manter Scalar apenas em Development.
```

### Entrega esperada

```text
- Bearer JWT aparece na documentação OpenAPI/Scalar;
- endpoints protegidos aparecem como protegidos, se suportado;
- build funcionando;
- commit: docs: document jwt bearer in openapi.
```

---

## Extra 05 — Resolver vulnerabilidade NU1903 / Microsoft.OpenApi

### Objetivo

Analisar e corrigir a vulnerabilidade transitiva `Microsoft.OpenApi 2.0.0`, se houver versão segura compatível.

### Escopo

Executar:

```bash
dotnet list package --vulnerable --include-transitive
```

Analisar pacotes relacionados:

```text
Microsoft.AspNetCore.OpenApi
Scalar.AspNetCore
Microsoft.OpenApi
```

### Regras

```text
- Não atualizar pacotes às cegas.
- Verificar origem da dependência transitiva.
- Preferir atualização compatível dos pacotes diretos.
- Não quebrar Scalar/OpenAPI.
- Não alterar código de domínio/aplicação.
```

### Entrega esperada

```text
- vulnerabilidade resolvida ou justificativa clara se não houver versão compatível;
- dotnet build funcionando;
- dotnet list package --vulnerable validado;
- commit: build: update vulnerable openapi dependency.
```

---

## Extra 06 — Corrigir PostgreSQL local, aplicar migration e validar seed admin

### Objetivo

Validar a API em runtime local com PostgreSQL real.

### Escopo

Executar, após corrigir credenciais locais fora do código versionado:

```bash
dotnet ef database update --context CourseCoreDbContext
```

Configurar seed por variável de ambiente:

```text
Seed__Admin__Enabled=true
Seed__Admin__Name=CourseCore Admin
Seed__Admin__Email=admin@coursecore.local
Seed__Admin__Password=<senha-local-nao-versionada>
Seed__Admin__ResetPassword=false
```

Rodar a aplicação e validar:

```text
- criação das tabelas;
- execução do seed;
- login admin;
- emissão de JWT;
- acesso ao /scalar.
```

### Regras

```text
- Não commitar senha real.
- Não commitar connection string real.
- Não alterar migration sem necessidade.
- Não recriar banco sem autorização.
- Não apagar dados existentes sem autorização.
```

### Entrega esperada

```text
- migration aplicada;
- seed admin validado;
- login admin funcionando;
- token JWT gerado;
- relatório de validação runtime;
- commit apenas se houver ajuste versionável necessário.
```

---

## Extra 07 — Melhorar refresh token com persistência real

### Objetivo

Implementar refresh token real com persistência, expiração, revogação e rotação.

### Escopo provável

Criar:

```text
RefreshToken Domain Entity
RefreshTokenPersistenceModel
RefreshTokenConfiguration
RefreshTokenRepository
RefreshTokenMapper
RefreshTokenHasher
RefreshTokenGenerator
Migration AddRefreshTokens
```

Alterar:

```text
JwtTokenService
JwtOptions
LoginUseCase
RefreshTokenUseCase
AuthDependencyInjection
CourseCoreDbContext
```

### Regras

```text
- Refresh token deve ser armazenado de forma segura.
- Armazenar apenas hash do refresh token.
- O token puro só deve ser retornado ao cliente.
- Deve ter expiração.
- Deve permitir revogação.
- Login deve emitir access token e refresh token.
- Refresh deve validar, rotacionar e revogar token antigo.
- Reutilização do refresh token antigo deve falhar com 401.
```

### Entrega esperada

```text
- refresh token funcional;
- migration criada;
- endpoints login/refresh funcionando;
- reutilização de refresh token antigo rejeitada;
- build funcionando;
- commit: feat: persist and rotate refresh tokens.
```

---

## Extra 08 — Criar testes automatizados principais

### Objetivo

Criar cobertura inicial de testes para reduzir regressão.

### Escopo

Criar projeto de testes, por exemplo:

```text
Tests/CourseCore.Api.Tests
```

Testar inicialmente:

```text
Domain Entities
ValueObjects
RefreshToken
LoginUseCase
RefreshTokenUseCase
CourseAccessService
Course
Lesson
UserAreaAccess
RoleAreaAccess
UserLessonProgress
UserCourseProgress
```

### Regras

```text
- Não depender de PostgreSQL real nos testes unitários.
- Usar fakes manuais para repositórios e serviços.
- Priorizar regras de domínio e use cases críticos.
- Separar testes unitários de testes de integração.
- dotnet test deve funcionar sem migrations, seed ou variáveis sensíveis.
```

### Entrega esperada

```text
- projeto de testes criado;
- dotnet test funcionando;
- testes cobrindo fluxos críticos;
- testes sem dependência de banco real;
- commit: test: add core domain and auth use case tests.
```

---

## Extra 09 — Otimizar localização de curso por aula

### Objetivo

Eliminar a busca ineficiente que varre cursos e detalhes para descobrir o curso relacionado a uma aula.

### Escopo

Avaliar e implementar método específico, por exemplo:

```text
ICourseRepository.FindByLessonIdAsync(Guid lessonId)
```

ou:

```text
ILessonRepository.FindCourseIdByLessonIdAsync(Guid lessonId)
```

Atualizar use cases que fazem varredura:

```text
RequestVideoPlaybackUseCase
RegisterLessonProgressUseCase
```

### Regras

```text
- Não quebrar contratos existentes sem necessidade.
- Preferir consulta eficiente via PersistenceModel/EF repository.
- Manter Application dependendo de interface.
- Não acessar DbContext no use case.
- Não alterar controllers, requests, responses ou presenters.
- Criar/ajustar testes unitários correspondentes.
```

### Entrega esperada

```text
- método otimizado criado;
- use cases atualizados;
- varredura removida;
- testes atualizados/criados;
- build/test funcionando;
- commit: refactor: optimize course lookup by lesson.
```

---

## Extra 10 — Padronizar status HTTP e CreatedAtAction

### Objetivo

Melhorar a aderência REST dos controllers.

### Escopo

Ajustar endpoints de criação para retornar status adequado:

```text
POST /api/users -> 201 Created
POST /api/courses -> 201 CreatedAtAction
POST /api/videos -> 201 Created
```

Avaliar também:

```text
- PUT retornar 200 ou 204 de forma consistente;
- endpoints de concessão retornarem 200, 201 ou 204 conforme regra;
- responses de erro já padronizadas pelo middleware;
- documentação via ProducesResponseType.
```

### Regras

```text
- Não alterar regra de negócio.
- Não alterar banco.
- Não alterar use cases, salvo se faltar dado essencial para CreatedAtAction.
- Não alterar rotas sem necessidade.
- Não adicionar try/catch local nos controllers.
- Preservar middleware global de exceções.
```

### Entrega esperada

```text
- status HTTP padronizados;
- endpoints de criação retornando 201 quando aplicável;
- ProducesResponseType adicionado/ajustado;
- build/test funcionando;
- commit: fix: standardize api response status codes.
```

---

## Extra 11 — Criar health checks e validações de runtime

### Objetivo

Adicionar endpoints de saúde para facilitar validação local e futura operação.

### Escopo

Configurar:

```text
AddHealthChecks
MapHealthChecks
Health check do processo
Health check do CourseCoreDbContext/PostgreSQL
```

Endpoints sugeridos:

```text
GET /health/live
GET /health/ready
GET /health
```

### Regras

```text
- Não expor detalhes sensíveis em produção.
- /health/live não deve depender do banco.
- /health/ready deve validar banco quando possível.
- Não quebrar startup local quando PostgreSQL estiver indisponível, salvo se readiness for chamado.
- Não misturar com lógica de negócio.
- Não alterar controllers.
```

### Entrega esperada

```text
- health check básico funcionando;
- /health/live disponível;
- /health/ready disponível;
- /health disponível;
- response JSON simples sem secrets;
- build/test funcionando;
- commit: feat: add health check endpoints.
```

---

## Extra 12 — Criar testes de integração HTTP básicos

### Objetivo

Criar uma primeira base de testes de integração HTTP para validar o comportamento real da API via controllers, middleware, autenticação e documentação.

### Escopo

Usar o projeto de testes existente:

```text
Tests/CourseCore.Api.Tests
```

Criar estrutura de integração, por exemplo:

```text
Tests/CourseCore.Api.Tests/Integration/Auth/
Tests/CourseCore.Api.Tests/Integration/Health/
Tests/CourseCore.Api.Tests/Integration/OpenApi/
Tests/CourseCore.Api.Tests/Integration/Infrastructure/
```

### Estratégia

```text
- Usar WebApplicationFactory<Program>.
- Usar SQLite in-memory para testes de integração.
- Manter conexão SQLite aberta durante os testes.
- Desabilitar seed real por configuração de teste.
- Inserir admin de teste diretamente no banco em memória.
- Não usar PostgreSQL real.
- Não executar migrations.
- Não executar database update.
```

### Testes mínimos

```text
GET /health/live -> 200
GET /health/ready -> 200
GET /health -> 200
GET /openapi/v1.json -> 200 e contém Bearer
GET /scalar -> 200
GET /api/users sem token -> 401
POST /api/auth/login com admin de teste -> 200
GET /api/users com token admin -> 200
POST /api/auth/refresh-token válido -> 200
reutilizar refresh token antigo -> 401
```

### Regras

```text
- Não usar PostgreSQL real nos testes.
- Não usar Testcontainers nesta etapa.
- Não alterar controllers.
- Não alterar rotas.
- Não alterar use cases.
- Não alterar appsettings.
- Não versionar segredo real.
- Se necessário, adicionar apenas public partial class Program ao Program.cs para WebApplicationFactory.
```

### Entrega esperada

```text
- testes HTTP básicos criados;
- WebApplicationFactory configurada;
- SQLite in-memory usado nos testes;
- auth/login/refresh validado via HTTP;
- health/OpenAPI/Scalar validados via HTTP;
- build/test funcionando;
- commit: test: add basic api integration tests.
```

---

## Extra 13 — Validar acesso do usuário em detalhes do curso

### Objetivo

Corrigir a falha de autorização funcional no endpoint de detalhes de curso.

Endpoint impactado:

```text
GET /api/courses/{courseId}
```

O endpoint já exige autenticação, mas deve também validar se o usuário autenticado possui acesso ao curso solicitado.

### Escopo

Alterar, se necessário:

```text
Modules/Courses/Application/DTOs/GetCourseDetailsInput.cs
Modules/Courses/Application/UseCases/GetCourseDetailsUseCase.cs
Modules/Courses/Presentation/Controllers/CoursesController.cs
Modules/Courses/Presentation/Presenters/CoursePresenter.cs
Tests/CourseCore.Api.Tests/Application/Courses/
```

### Regras

```text
- O UserId deve vir do JWT via ICurrentUserService.
- Não aceitar userId por query/body.
- Preservar a rota GET /api/courses/{courseId}.
- Curso inexistente deve retornar 404.
- Usuário sem acesso deve retornar 403.
- Usuário com acesso deve retornar 200.
- Usar CourseAccessService ou regra central já existente.
- Não duplicar regra de acesso no controller.
- Controller não deve acessar DbContext.
- UseCase não deve acessar DbContext.
```

### Testes esperados

```text
- curso inexistente deve lançar NotFoundException;
- usuário sem acesso deve lançar ForbiddenException;
- usuário com acesso deve retornar detalhes;
- o UserId usado deve vir do input montado com usuário autenticado;
- GET /api/courses/{courseId} sem token continua 401, se houver teste HTTP simples.
```

### Não deve fazer

```text
- Não alterar rotas.
- Não criar endpoint novo.
- Não alterar controllers fora de CoursesController.
- Não alterar Domain Entities.
- Não alterar PersistenceModels.
- Não criar migration.
- Não executar database update.
- Não rodar seed.
- Não alterar appsettings.
```

### Entrega esperada

```text
- GetCourseDetails valida acesso funcional;
- usuário sem acesso recebe 403;
- usuário com acesso recebe detalhes;
- testes criados/ajustados;
- build/test funcionando;
- commit: fix: enforce access checks on course details.
```

---

## Extra 14 — Revisão final para ambiente de produção

### Objetivo

Revisar o projeto pensando em publicação, segurança, operação e manutenção.

Esta etapa deve começar como análise/relatório. Código só deve ser alterado depois que os próximos ajustes forem definidos.

### Escopo da análise

Codex deve gerar relatório sem alterar código inicialmente, validando:

```text
- secrets/configurações;
- JWT secret fora do appsettings em produção;
- connection strings;
- CORS;
- HTTPS;
- proxy/reverse proxy;
- logs estruturados;
- health checks;
- migrations;
- seed;
- Scalar apenas em Development;
- OpenAPI security scheme;
- endpoints protegidos;
- userId não aceito do cliente em fluxos sensíveis;
- validação de acesso funcional em detalhes de curso;
- vulnerabilidades de pacotes;
- testes unitários;
- testes de integração;
- readiness para deploy;
- riscos restantes.
```

### Regras

```text
- Começar com relatório sem alterar código.
- Não corrigir tudo em um único prompt.
- Separar próximos ajustes por etapa/commit.
- Não versionar secrets.
- Não executar database update sem instrução explícita.
- Não rodar seed sem instrução explícita.
```

### Entrega esperada

```text
- relatório de readiness;
- riscos restantes;
- checklist de produção;
- lista priorizada de ajustes finais;
- recomendação da próxima etapa.
```

---

# 6. Checklist permanente das etapas extras

Durante as etapas extras, verificar continuamente:

```text
[ ] Build passa.
[ ] Testes passam, quando existirem testes no projeto.
[ ] Git status está limpo antes de iniciar nova etapa.
[ ] Commit representa exatamente a mudança feita.
[ ] Nenhuma senha real foi versionada.
[ ] Nenhuma connection string real foi versionada.
[ ] Nenhum token/segredo real foi versionado.
[ ] Controllers não acessam DbContext.
[ ] Controllers não acessam repositórios EF concretos.
[ ] Use cases não acessam DbContext.
[ ] Domain não depende de Infrastructure.
[ ] DbContext usa apenas PersistenceModels.
[ ] Scalar fica somente em Development.
[ ] Endpoints sensíveis exigem autenticação.
[ ] Fluxos sensíveis usam usuário autenticado, não userId vindo do cliente.
[ ] Detalhes de curso validam acesso funcional do usuário.
[ ] Refresh token é armazenado como hash, não token puro.
[ ] Refresh token antigo não pode ser reutilizado após rotação.
[ ] Health live não depende do banco.
[ ] Health ready valida banco quando aplicável.
[ ] Migration só é criada quando a etapa pedir.
[ ] Database update só é executado quando a etapa pedir.
[ ] Seed só é executado quando a etapa pedir.
[ ] dotnet list package --vulnerable --include-transitive não aponta vulnerabilidades conhecidas.
```

---

# 7. Observação final

As etapas extras devem ser tratadas como melhorias pós-plano principal.

Não usar a nomenclatura `posição 23`, `posição 24` ou semelhante, pois o plano principal foi encerrado na posição 22.

A nomenclatura correta é:

```text
Extra 01
Extra 02
Extra 03
...
```

Cada etapa extra deve ser executada, validada e commitada separadamente.

Se uma nova melhoria surgir fora desta lista, ela deve ser adicionada como uma nova Extra posterior, sem renumerar o histórico já executado.
