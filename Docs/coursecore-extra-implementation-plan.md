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
- autenticação base com JWT;
- BCryptPasswordHasher;
- use cases principais;
- requests, responses e presenters;
- controllers modulares;
- migration inicial;
- middleware global de exceções;
- exceções específicas de Application;
- seed inicial opt-in;
- Scalar/OpenAPI parcialmente configurado.
```

Pendências conhecidas:

```text
- PostgreSQL local ainda pode falhar por credencial 28P01;
- endpoints sensíveis ainda recebem userId do cliente;
- autorização real ainda precisa ser aplicada em todos os controllers;
- ICurrentUserService ainda precisa ser implementado/registrado, se não tiver sido feito;
- JWT Bearer ainda não está documentado explicitamente no Scalar/OpenAPI;
- refresh token ainda não possui persistência real;
- vulnerabilidade transitiva Microsoft.OpenApi 2.0.0 ainda precisa ser revisada;
- testes automatizados ainda precisam ser criados.
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
```

---

## 4. Ordem recomendada das etapas extras

```text
Extra 01 - Configurar Scalar/OpenAPI documentation
Extra 02 - Implementar CurrentUserService, policies e proteção dos controllers
Extra 03 - Remover userId sensível dos requests e usar usuário autenticado
Extra 04 - Documentar JWT Bearer no Scalar/OpenAPI
Extra 05 - Resolver vulnerabilidade NU1903 / Microsoft.OpenApi
Extra 06 - Corrigir PostgreSQL local, aplicar migration e validar seed admin
Extra 07 - Melhorar refresh token com persistência real
Extra 08 - Otimizar localização de curso por aula
Extra 09 - Padronizar status HTTP e CreatedAtAction
Extra 10 - Criar testes automatizados principais
Extra 11 - Criar health checks e validações de runtime
Extra 12 - Revisão final para ambiente de produção
```

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
- commit: refactor: use authenticated user in user-scoped endpoints.
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
- commit: docs: document jwt bearer authentication.
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
- commit: build: update openapi dependencies.
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

Implementar refresh token real com persistência, expiração e revogação.

### Escopo provável

Criar:

```text
RefreshToken Domain/PersistenceModel, se fizer sentido
RefreshTokenConfiguration
RefreshTokenRepository
RefreshTokenMapper
Migration nova
Atualização do JwtTokenService
Atualização do LoginUseCase
Atualização do RefreshTokenUseCase
```

### Regras

```text
- Refresh token deve ser armazenado de forma segura.
- Não armazenar token puro se for possível armazenar hash.
- Deve ter expiração.
- Deve permitir revogação.
- Login deve emitir access token e refresh token.
- Refresh deve validar, rotacionar e revogar token antigo.
```

### Entrega esperada

```text
- refresh token funcional;
- migration criada;
- endpoints login/refresh funcionando;
- build funcionando;
- commit dividido por contexto se houver migration + código.
```

---

## Extra 08 — Otimizar localização de curso por aula

### Objetivo

Eliminar a busca ineficiente que varre cursos e detalhes para descobrir o curso relacionado a uma aula.

### Escopo

Avaliar e implementar método específico, por exemplo:

```text
ICourseRepository.FindCourseIdByLessonIdAsync(Guid lessonId)
```

ou:

```text
ILessonRepository.FindCourseIdByLessonIdAsync(Guid lessonId)
```

Atualizar use cases que hoje fazem varredura:

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
```

### Entrega esperada

```text
- método otimizado criado;
- use cases atualizados;
- build funcionando;
- commit: perf: optimize course lookup by lesson.
```

---

## Extra 09 — Padronizar status HTTP e CreatedAtAction

### Objetivo

Melhorar a aderência REST dos controllers.

### Escopo

Ajustar endpoints de criação para retornar status adequado:

```text
POST /api/users -> 201 Created
POST /api/courses -> 201 Created
POST /api/videos -> 201 Created
```

Avaliar também:

```text
- PUT retornar 200 ou 204 de forma consistente;
- endpoints de concessão retornarem 200, 201 ou 204 conforme regra;
- responses de erro já padronizadas pelo middleware.
```

### Regras

```text
- Não alterar regra de negócio.
- Não alterar banco.
- Não alterar use cases, salvo se faltar dado essencial para CreatedAtAction.
- Não alterar rotas sem necessidade.
```

### Entrega esperada

```text
- status HTTP padronizados;
- build funcionando;
- commit: refactor: standardize api response status codes.
```

---

## Extra 10 — Criar testes automatizados principais

### Objetivo

Criar cobertura inicial de testes para reduzir regressão.

### Escopo

Criar projeto de testes, por exemplo:

```text
CourseCore.Tests
```

Testar inicialmente:

```text
Domain Entities
ValueObjects
CourseAccessService
CreateCourseUseCase
RegisterLessonProgressUseCase
ExceptionHandlingMiddleware, se viável
```

### Regras

```text
- Não depender de PostgreSQL real nos testes unitários.
- Usar mocks/fakes para repositórios.
- Priorizar regras de domínio e use cases críticos.
- Separar testes unitários de testes de integração.
```

### Entrega esperada

```text
- projeto de testes criado;
- dotnet test funcionando;
- primeiros testes cobrindo fluxos críticos;
- commit: test: add core application tests.
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
health check do PostgreSQL, se pacote compatível for usado
```

Endpoint sugerido:

```text
GET /health
```

### Regras

```text
- Não expor detalhes sensíveis em produção.
- Não quebrar startup local quando PostgreSQL estiver indisponível, salvo se health check for chamado.
- Não misturar com lógica de negócio.
```

### Entrega esperada

```text
- health check básico funcionando;
- build funcionando;
- commit: feat: add api health checks.
```

---

## Extra 12 — Revisão final para ambiente de produção

### Objetivo

Revisar o projeto pensando em publicação, segurança e manutenção.

### Escopo

Codex deve gerar relatório sem alterar código inicialmente, validando:

```text
- secrets/configurações;
- JWT secret fora do appsettings;
- CORS;
- HTTPS;
- logs;
- health checks;
- migrations;
- seed;
- Scalar apenas em Development;
- endpoints protegidos;
- userId não aceito do cliente em fluxos sensíveis;
- vulnerabilidades de pacotes;
- testes;
- readiness para deploy.
```

### Entrega esperada

```text
- relatório de readiness;
- riscos restantes;
- checklist de produção;
- próximos ajustes recomendados.
```

---

# 6. Checklist permanente das etapas extras

Durante as etapas extras, verificar continuamente:

```text
[ ] Build passa.
[ ] Git status está limpo antes de iniciar nova etapa.
[ ] Commit representa exatamente a mudança feita.
[ ] Nenhuma senha real foi versionada.
[ ] Nenhuma connection string real foi versionada.
[ ] Controllers não acessam DbContext.
[ ] Controllers não acessam repositórios EF concretos.
[ ] Use cases não acessam DbContext.
[ ] Domain não depende de Infrastructure.
[ ] DbContext usa apenas PersistenceModels.
[ ] Scalar fica somente em Development.
[ ] Endpoints sensíveis exigem autenticação.
[ ] Fluxos sensíveis usam usuário autenticado, não userId vindo do cliente.
[ ] Migration só é criada quando a etapa pedir.
[ ] Database update só é executado quando a etapa pedir.
[ ] Seed só é executado quando a etapa pedir.
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
