# CourseCore API — Planejamento Pós-Readiness com Codex

## 1. Objetivo do documento

Este documento define as próximas etapas extras recomendadas para o backend **CourseCore API** após a conclusão da revisão final de readiness.

O plano principal foi concluído nas 22 posições previstas.

O plano de etapas extras também avançou até:

```text
Extra 14 — Revisão final para ambiente de produção
```

A partir deste documento, as próximas etapas devem ser tratadas como **pós-readiness**, mantendo a nomenclatura:

```text
Extra 15
Extra 16
Extra 17
...
```

Este documento deve ser usado como referência para orientar os próximos prompts enviados ao Codex.

---

## 2. Estado atual do projeto

O projeto já possui:

```text
- arquitetura Clean Architecture / DDD modular;
- separação entre Domain Entity e PersistenceModel;
- EF Core/PostgreSQL com migrations;
- seed admin opt-in;
- autenticação JWT;
- refresh token persistido com hash, expiração, revogação e rotação;
- CurrentUserService;
- policies básicas;
- endpoints protegidos;
- userId sensível removido dos requests de consumo;
- validação de acesso nos detalhes do curso;
- Scalar/OpenAPI com Bearer;
- vulnerabilidade NU1903 corrigida;
- health checks;
- testes unitários;
- testes de integração HTTP com SQLite in-memory;
- status HTTP padronizados;
- build sem warnings;
- testes passando;
- pacotes sem vulnerabilidades conhecidas.
```

A revisão de readiness concluiu que o projeto está:

```text
- pronto para desenvolvimento local;
- bem encaminhado para homologação;
- ainda não pronto para produção.
```

Principais bloqueadores antes de produção:

```text
- secrets e connection string ainda dependem de configuração local/versionada;
- CORS ainda precisa ser configurado explicitamente;
- HTTPS/HSTS/reverse proxy ainda precisam de hardening;
- logs estruturados e correlação ainda não estão implementados;
- Docker/CI/CD ainda não estão definidos;
- policies ainda são baseadas apenas em role Admin;
- OpenAPI aplica Bearer globalmente, inclusive em endpoints AllowAnonymous.
```

---

## 3. Regras fixas para as próximas etapas

Em todas as próximas etapas, o Codex deve respeitar:

```text
1. Considerar somente o estado atual do repositório.
2. Ler Docs/coursecore-extra-implementation-plan.md antes de alterar código.
3. Ler este documento antes de criar prompts das etapas pós-readiness.
4. Não implementar fora do escopo da etapa atual.
5. Não misturar mais de uma etapa no mesmo commit.
6. Não commitar senha, JWT secret real, token real ou connection string real.
7. Não alterar migrations quando a etapa não pedir isso.
8. Não executar database update quando a etapa não pedir isso.
9. Não rodar seed quando a etapa não pedir isso.
10. Não alterar controllers/use cases/domínio fora do escopo.
11. Rodar dotnet restore, dotnet build e dotnet test ao final.
12. Rodar dotnet list package --vulnerable --include-transitive.
13. Só commitar se build/test passarem e o diff estiver dentro do escopo.
14. Usar Conventional Commits.
15. A mensagem do commit deve descrever a mudança real, não apenas o número da etapa.
16. Informar arquivos criados, alterados, removidos, warnings, erros e pendências.
```

---

## 4. Ordem recomendada das próximas etapas

```text
Extra 15 - Hardening de configuração para produção
Extra 16 - Logs estruturados, correlation id e observabilidade
Extra 17 - Dockerfile e docker-compose para ambiente local/homologação
Extra 18 - Pipeline CI para restore, build, test e vulnerability check
Extra 19 - Refinar OpenAPI Bearer por endpoint
Extra 20 - Evoluir policies de Admin para permissions/claims
Extra 21 - Expandir testes HTTP dos módulos administrativos
Extra 22 - Estratégia de migrations/deploy
Extra 23 - Audit logs de ações sensíveis
Extra 24 - Revisão final pós-hardening
```

---

# 5. Planejamento por etapa

---

## Extra 15 — Hardening de configuração para produção

### Objetivo

Preparar a configuração da API para homologação/produção, evitando dependência de secrets versionados e configurando segurança básica de hosting.

### Escopo

Tratar:

```text
- connection string via variável de ambiente;
- JWT secret obrigatório em Production;
- JWT issuer/audience obrigatórios em Production;
- CORS explícito por ambiente;
- HTTPS redirection;
- HSTS fora de Development;
- documentação de variáveis de ambiente;
- validação de configuração crítica em Production.
```

Arquivos prováveis:

```text
Program.cs
appsettings.json
appsettings.Development.json
Docs/production-configuration.md
Shared/Infrastructure/Configuration/
Tests/CourseCore.Api.Tests/
```

### Regras

```text
- Não commitar segredo real.
- Não usar AllowAnyOrigin em Production.
- Não expor Scalar/OpenAPI em Production.
- Não rodar seed.
- Não executar database update.
- Não criar migration.
- Preservar experiência local em Development.
```

### Entrega esperada

```text
- configuração crítica validada em Production;
- CORS configurado por ambiente;
- HSTS/HTTPS revisados;
- documentação de produção criada;
- build/test passando;
- commit: chore: harden production configuration.
```

---

## Extra 16 — Logs estruturados, correlation id e observabilidade

### Objetivo

Melhorar a rastreabilidade da API para homologação e produção.

### Escopo

Avaliar e implementar:

```text
- logging estruturado;
- request id/correlation id;
- inclusão de traceId/correlationId nos logs;
- logs no middleware global de exceções;
- logs básicos nos eventos de autenticação/refresh token, sem expor tokens;
- documentação de observabilidade.
```

Arquivos prováveis:

```text
Program.cs
Shared/Presentation/Middleware/ExceptionHandlingMiddleware.cs
Shared/Presentation/Responses/ApiErrorResponse.cs
Shared/Presentation/Middleware/CorrelationIdMiddleware.cs
Docs/observability.md
Tests/CourseCore.Api.Tests/
```

### Regras

```text
- Não logar access token.
- Não logar refresh token.
- Não logar senha.
- Não logar connection string.
- Não expor stack trace em Production.
- Não trocar toda a stack de logging sem necessidade.
```

### Entrega esperada

```text
- correlation id por request;
- erros logados com contexto seguro;
- ApiErrorResponse mantendo traceId/correlationId;
- testes ajustados/criados;
- documentação de observabilidade;
- commit: feat: add structured request observability.
```

---

## Extra 17 — Dockerfile e docker-compose para ambiente local/homologação

### Objetivo

Criar uma base de containerização para rodar a API e PostgreSQL de forma reproduzível.

### Escopo

Criar:

```text
Dockerfile
.dockerignore
docker-compose.yml
docker-compose.override.yml, se fizer sentido
Docs/docker.md
```

Serviços esperados:

```text
coursecore-api
coursecore-postgres
```

### Regras

```text
- Não colocar segredo real no docker-compose.
- Usar variáveis de ambiente.
- Não rodar migrations automaticamente no container sem decisão explícita.
- Não habilitar seed por padrão em Production.
- Manter Scalar apenas em Development.
```

### Entrega esperada

```text
- API buildável em container;
- PostgreSQL local via compose;
- documentação de execução;
- build/test locais continuam passando;
- commit: chore: add docker development environment.
```

---

## Extra 18 — Pipeline CI para restore, build, test e vulnerability check

### Objetivo

Criar pipeline de CI para validar o projeto automaticamente a cada push/pull request.

### Escopo

Criar workflow, por exemplo:

```text
.github/workflows/ci.yml
```

Etapas mínimas:

```text
- checkout;
- setup .NET;
- restore;
- build;
- test;
- dotnet list package --vulnerable --include-transitive.
```

### Regras

```text
- Não usar segredo real.
- Não depender de PostgreSQL real.
- Usar testes com SQLite in-memory.
- Não rodar migrations.
- Não rodar seed real.
```

### Entrega esperada

```text
- pipeline CI criado;
- documentação básica, se necessário;
- comandos locais continuam passando;
- commit: ci: add dotnet validation workflow.
```

---

## Extra 19 — Refinar OpenAPI Bearer por endpoint

### Objetivo

Corrigir a documentação OpenAPI para que endpoints `[AllowAnonymous]` não apareçam como protegidos por Bearer quando não precisam.

### Escopo

Alterar:

```text
Shared/Presentation/OpenApi/BearerSecuritySchemeTransformer.cs
```

Possivelmente analisar metadata dos endpoints:

```text
AuthorizeAttribute
AllowAnonymousAttribute
```

### Regras

```text
- Não alterar autenticação real.
- Não alterar controllers, salvo se estritamente necessário para metadados.
- Não alterar rotas.
- Não expor Scalar em Production.
- Não remover Bearer dos endpoints protegidos.
```

### Entrega esperada

```text
- login/refresh aparecem como públicos na documentação;
- endpoints protegidos continuam com Bearer;
- OpenAPI/Scalar funcionando;
- testes de integração ajustados;
- commit: docs: refine openapi bearer requirements.
```

---

## Extra 20 — Evoluir policies de Admin para permissions/claims

### Objetivo

Evoluir autorização de policies baseadas somente em role `Admin` para permissions/claims mais granulares.

### Escopo

Avaliar e implementar:

```text
- emissão de permission claims no JWT;
- policies baseadas em permissions;
- preservação da role Admin;
- vínculo com permissões já existentes no seed;
- atualização das policies ManageUsers, ManageAccess, ManageCourses, ManageVideos, ReadProgress.
```

Arquivos prováveis:

```text
Modules/Auth/
Modules/Access/
Shared/Infrastructure/Security/
Modules/Auth/AuthDependencyInjection.cs
Shared/Infrastructure/Persistence/Seed/
Tests/CourseCore.Api.Tests/
```

### Regras

```text
- Não quebrar login admin.
- Não remover suporte à role Admin sem necessidade.
- Não aceitar permissions vindas do cliente.
- Claims devem vir do banco/usuário autenticado.
- Não criar migration se o modelo atual já suportar permissions.
```

### Entrega esperada

```text
- JWT contém permissions;
- policies validam permissions;
- seed admin continua concedendo permissões;
- testes unitários/integração atualizados;
- commit: feat: authorize endpoints with permission claims.
```

---

## Extra 21 — Expandir testes HTTP dos módulos administrativos

### Objetivo

Aumentar cobertura dos endpoints HTTP administrativos e principais fluxos de erro.

### Escopo

Adicionar testes de integração para:

```text
- POST /api/users;
- PUT /api/users/{userId};
- POST /api/access/user-area;
- POST /api/access/role-area;
- POST /api/courses;
- PUT /api/courses/{courseId};
- POST /api/courses/{courseId}/publish;
- POST /api/videos;
- POST /api/videos/playback;
- POST /api/progress/lessons;
- POST /api/progress/courses.
```

### Regras

```text
- Usar WebApplicationFactory existente.
- Usar SQLite in-memory.
- Não usar PostgreSQL real.
- Não rodar seed real.
- Não executar migrations.
- Não alterar regra de negócio apenas para facilitar teste.
```

### Entrega esperada

```text
- testes HTTP cobrindo fluxos administrativos;
- cenários 200/201/400/401/403/404 quando aplicável;
- dotnet test passando;
- commit: test: expand api integration coverage.
```

---

## Extra 22 — Estratégia de migrations/deploy

### Objetivo

Definir uma estratégia segura para aplicar migrations em homologação/produção.

### Escopo

Criar documentação e scripts seguros, sem aplicar migrations automaticamente no startup.

Possíveis arquivos:

```text
Docs/deployment-migrations.md
scripts/
```

Conteúdo esperado:

```text
- como gerar migration;
- como revisar migration;
- como aplicar em homologação;
- como aplicar em produção;
- como gerar script SQL idempotente;
- como fazer rollback planejado;
- por que não aplicar migration automática no startup por padrão.
```

### Regras

```text
- Não executar database update.
- Não criar migration nova.
- Não apagar dados.
- Não colocar secrets nos scripts.
```

### Entrega esperada

```text
- documentação de migrations/deploy;
- scripts opcionais sem secrets;
- commit: docs: add migration deployment strategy.
```

---

## Extra 23 — Audit logs de ações sensíveis

### Objetivo

Ativar uso prático do módulo `AuditLogs` para registrar ações sensíveis da API.

### Escopo

Avaliar e registrar eventos como:

```text
- login bem-sucedido;
- refresh token rotacionado;
- refresh token reutilizado/rejeitado;
- criação/alteração de usuário;
- concessão de acesso a usuário;
- concessão de acesso a role;
- criação/edição/publicação de curso;
- criação de vídeo.
```

Arquivos prováveis:

```text
Modules/AuditLogs/
Modules/Auth/
Modules/Users/
Modules/Access/
Modules/Courses/
Modules/Media/
Shared/Application/Contracts/ICurrentUserService.cs
Tests/CourseCore.Api.Tests/
```

### Regras

```text
- Não registrar senha.
- Não registrar access token.
- Não registrar refresh token puro.
- Não registrar connection string.
- Registrar apenas metadados seguros.
- Usar repository/interface já existente.
- Não acessar DbContext diretamente nos use cases.
```

### Entrega esperada

```text
- ações sensíveis registradas em audit_logs;
- testes cobrindo eventos principais;
- build/test passando;
- commit: feat: audit sensitive application actions.
```

---

## Extra 24 — Revisão final pós-hardening

### Objetivo

Revisar o projeto novamente após as etapas pós-readiness.

### Escopo

Codex deve gerar relatório sem alterar código, validando:

```text
- configuração de produção;
- CORS;
- HTTPS/HSTS;
- logs/correlation id;
- Docker;
- CI;
- OpenAPI;
- permissions/claims;
- testes;
- migrations/deploy;
- audit logs;
- secrets;
- readiness para homologação e produção.
```

### Regras

```text
- Não alterar arquivos.
- Não criar arquivos.
- Não commitar.
- Não executar database update.
- Não rodar seed.
```

### Entrega esperada

```text
- relatório final pós-hardening;
- riscos restantes;
- checklist de produção;
- próximos ajustes se existirem.
```

---

# 6. Checklist permanente pós-readiness

Antes de encerrar qualquer etapa:

```text
[ ] git status estava limpo antes de iniciar.
[ ] A etapa atual está documentada no prompt.
[ ] O diff está dentro do escopo.
[ ] Nenhum segredo real foi versionado.
[ ] Nenhuma connection string real foi versionada.
[ ] Nenhuma migration foi criada sem a etapa pedir.
[ ] Nenhum database update foi executado sem a etapa pedir.
[ ] Seed real não foi rodado sem a etapa pedir.
[ ] dotnet restore passou.
[ ] dotnet build passou.
[ ] dotnet test passou.
[ ] dotnet list package --vulnerable --include-transitive passou.
[ ] Commit usa Conventional Commits.
[ ] Commit descreve a mudança real.
```

---

# 7. Observação final

Este documento não substitui o planejamento original nem o plano de extras já executado.

Ele serve apenas para orientar as próximas melhorias pós-readiness.

Cada etapa deve ser executada, validada e commitada separadamente.
