# CourseCore — Roadmap de correções de segurança

> Destino sugerido no repositório: `Docs/security-hardening-roadmap.md`.

## 1. Objetivo

Este documento consolida o plano de correção dos problemas de segurança identificados no CourseCore API.

Ele deve ser usado como documento-base para criar prompts posteriores para o Codex, sem transformar cada vulnerabilidade em um arquivo isolado.

## 2. Escopo

O plano cobre:

- autenticação;
- autorização;
- refresh tokens;
- cookies HttpOnly;
- rate limiting;
- controle de sessão;
- regras de negócio sensíveis;
- URLs de vídeo;
- validações de payload;
- Docker/configuração operacional;
- health checks;
- endpoints e contratos com impacto de segurança.

Este documento **não é um pentest completo**. Ele parte de uma análise estática do estado atual do projeto e deve ser complementado futuramente por testes dinâmicos, DAST, teste de carga, revisão de infraestrutura e validação do provedor de storage.

## 3. Princípios de implementação

Todas as correções devem seguir os princípios abaixo:

1. Preservar a arquitetura modular atual.
2. Não colocar regra de negócio em controllers.
3. Não expor `DbContext` fora da Infrastructure.
4. Não usar `PersistenceModel` fora da Infrastructure.
5. Não chamar `SaveChanges` diretamente em repositories ou serviços de aplicação.
6. Preservar `UnitOfWork`.
7. Não versionar secrets.
8. Não alterar `.env`.
9. Não rodar seed real sem solicitação explícita.
10. Não aplicar migrations automaticamente no startup.
11. Criar testes antes ou junto da correção.
12. Manter compatibilidade com Postman quando possível.
13. Preferir mudanças incrementais com commits pequenos.
14. Usar Conventional Commits.
15. Validar sempre:

```bash
dotnet restore
dotnet build
dotnet test
dotnet list package --vulnerable --include-transitive
git status
git status --ignored
git diff --stat
```

## 4. Classificação geral dos riscos

| Prioridade | Grupo | Itens principais | Bloqueia produção? |
|---|---|---|---|
| P0 | Auth, sessão e autorização crítica | roles inativas, refresh token atômico, rate limiting, invalidação de JWT, conclusão falsa, vídeo, Docker secrets | Sim |
| P1 | Hardening de autenticação e API | senha forte, timing attack, cookie HttpOnly, logout, payload limits, exceções, policies específicas | Sim, se a aplicação for pública |
| P2 | Integridade funcional e escalabilidade | paginação, N+1, idempotência, verbos HTTP, preview, validação de publicação | Não sempre, mas deve entrar antes de escala |
| P3 | Operação e observabilidade externa | health checks internos, métricas, tracing, logging externo, documentação final | Recomendado |

## 5. Sequência recomendada de prompts

Abaixo está a sequência sugerida para implementar as correções sem misturar riscos demais em um único prompt.

### Prompt 01 — Autorização com roles ativas

Corrigir:

- H-01 — funções desativadas continuam concedendo autorização;
- parte de I-09 — concessões para função inativa;
- cobertura de `CourseAccessService`.

Objetivo:

- `FindByUserIdAsync` deve retornar somente roles ativas;
- acesso por role em curso deve considerar somente roles ativas;
- grants de acesso para role inativa devem ser bloqueados ou tratados explicitamente;
- adicionar testes para `Admin` inativa, role comum inativa e role-area access inativo.

Risco de migration: baixo. Provavelmente não exige migration.

### Prompt 02 — Refresh token atômico, replay e logout

Corrigir:

- H-02 — condição de corrida no refresh token;
- M-04 — ausência de detecção de reutilização da família de refresh tokens;
- parte da estratégia de sessão.

Objetivo:

- rotação de refresh token deve ser atômica;
- refresh token revogado/reutilizado deve acionar detecção de replay;
- implementar `FamilyId`/`ParentTokenId` ou estratégia equivalente;
- revogar família de tokens quando reutilização for detectada;
- criar endpoint de logout;
- criar endpoint de revogar todas as sessões, se fizer sentido.

Risco de migration: alto. Pode exigir migration para `FamilyId`, `ParentTokenId`, `RevokedReason` ou `TokenVersion`.

### Prompt 03 — Cookies HttpOnly para refresh token

Corrigir:

- M-03 — refresh token devolvido no corpo JSON para aplicações web.

Objetivo recomendado:

- login continua retornando access token no body;
- refresh token passa a ser setado em cookie `HttpOnly`;
- refresh endpoint lê cookie por padrão;
- manter fallback por body para Postman/mobile, se desejado;
- logout limpa cookie e revoga token;
- configurar `Secure`, `SameSite`, `Path`, `MaxAge`;
- documentar CSRF e CORS com credentials.

Risco de migration: baixo se aproveitar estrutura atual de refresh token.

Decisão arquitetural recomendada:

```text
Access token: body e memória no frontend.
Refresh token: cookie HttpOnly.
Fallback body: opcional para Postman/mobile.
```

### Prompt 04 — Rate limiting e proteção de login/refresh

Corrigir:

- H-03 — ausência de rate limiting em login e refresh;
- M-02 — enumeração por tempo de resposta;
- parte de DoS por BCrypt e auditoria.

Objetivo:

- `AddRateLimiter` e `UseRateLimiter`;
- política específica para login;
- política específica para refresh;
- política geral para endpoints autenticados;
- chave por IP e, no login, por e-mail normalizado quando possível;
- BCrypt fictício quando usuário não existir;
- limitar tamanho de body;
- auditar sem permitir crescimento abusivo de registros por tokens inválidos.

Risco de migration: baixo.

### Prompt 05 — Invalidação de JWT por alteração de usuário/privilégios

Corrigir:

- H-05 — usuário/função/permissão desativada não invalida JWT atual;
- parte de H-01 e M-04.

Objetivo:

- adicionar `TokenVersion` ou `SecurityStamp` ao usuário;
- emitir versão no JWT;
- validar versão em endpoints sensíveis;
- incrementar versão ao desativar usuário, trocar senha, alterar roles/permissões;
- revogar refresh tokens ativos nesses eventos;
- reduzir `AccessTokenExpirationMinutes` para 5 a 15 minutos.

Risco de migration: alto. Exige coluna nova em usuário ou tabela equivalente.

### Prompt 06 — Progresso de aula controlado pelo servidor

Corrigir:

- H-04 — usuário pode falsificar conclusão de aula/curso.

Objetivo:

- remover confiança em `markAsCompleted` vindo do cliente;
- servidor calcula conclusão com base em duração real;
- `WatchedSeconds` deve ser monotônico;
- limitar `WatchedSeconds` à duração do vídeo;
- exigir percentual mínimo, por exemplo 80% ou 90%;
- impedir saltos incompatíveis com tempo transcorrido, se houver sessão de playback;
- ajustar contrato com compatibilidade ou versionamento.

Risco de migration: médio. Pode exigir campos de eventos/sessão se a validação for mais forte.

### Prompt 07 — URLs de vídeo assinadas e temporárias

Corrigir:

- H-06 — modelo inseguro de URL de vídeo;
- parte de free preview e segurança de storage.

Objetivo:

- não aceitar `PlaybackUrl` arbitrária como fonte de verdade;
- criar serviço de geração de URL assinada;
- validar `StorageProvider`;
- validar `StorageKey`;
- gerar URL temporária no playback;
- evitar persistir URL pública longa;
- criar configuração segura por provider;
- impedir audit/log de `storageKey` sensível ou URL assinada.

Risco de migration: médio. Pode exigir ajuste em modelo de vídeo se `PlaybackUrl` precisar ser removida ou reinterpretada.

### Prompt 08 — Docker e ambiente operacional seguro

Corrigir:

- H-07 — Docker Compose com defaults inseguros;
- parte de health checks expostos.

Objetivo:

- remover defaults inseguros para JWT e senha do Postgres;
- falhar se variáveis obrigatórias não forem informadas;
- separar compose local e compose produção;
- manter Postgres sem porta exposta em perfil não local;
- usar usuário não-root no container da API;
- documentar HTTPS/reverse proxy;
- revisar health checks públicos.

Risco de migration: baixo.

### Prompt 09 — Validação de senha, payloads e exceções

Corrigir:

- M-01 — senha insuficiente;
- M-07 — payloads sem limites;
- M-08 — `InvalidOperationException` pode vazar mensagem interna.

Objetivo:

- serviço central de política de senha;
- limite mínimo e máximo;
- validação contra senhas comuns/vazadas como pendência ou integração futura;
- limites de strings alinhados ao banco;
- limites de coleções aninhadas;
- limite de tamanho de body;
- validação de URL;
- tratar `InvalidOperationException` desconhecida como 500;
- usar exceções de domínio/aplicação para 400 conhecido.

Risco de migration: baixo.

### Prompt 10 — Policies específicas e contratos de acesso

Corrigir:

- M-09 — `ManageAccess` ampla demais;
- I-05 — endpoint de verificação de acesso incoerente.

Objetivo:

- criar policies específicas:
  - `ManageUserAreaAccess`;
  - `ManageRoleAreaAccess`;
  - `CheckOwnCourseAccess`;
  - `CheckUserCourseAccess`, se houver endpoint administrativo;
- ajustar controller sem quebrar autorização;
- separar endpoint de consulta própria e consulta administrativa;
- atualizar OpenAPI e Postman.

Risco de migration: baixo.

### Prompt 11 — Paginação, N+1 e idempotência

Corrigir:

- M-05 — N+1 em cursos disponíveis;
- M-06 — listagem completa de usuários sem paginação;
- I-09 — concessões de acesso não idempotentes.

Objetivo:

- `GET /api/users?page=1&pageSize=50`;
- limite máximo de `pageSize`;
- consulta única para cursos disponíveis;
- grants idempotentes ou retorno 409 controlado;
- testes de carga básicos ou testes de query count, se possível.

Risco de migration: baixo/médio dependendo de índices adicionais.

### Prompt 12 — Regras funcionais com impacto de segurança

Corrigir:

- I-01 — `Active` ausente desativa usuário;
- I-02 — e-mail não verificado;
- I-03 — free preview não funciona;
- I-06 — consulta de progresso usa POST;
- I-07 — publicação sem validação;
- I-08 — ausência de endpoints para módulos/aulas;
- I-10 — health detalhado público.

Objetivo:

- `Active` nullable ou PATCH;
- proteger último admin e auto-desativação;
- decidir regra de e-mail verificado;
- implementar preview gratuito;
- validar curso antes de publicar;
- criar endpoints de módulos/aulas, se entrar no escopo;
- separar health público mínimo e health interno detalhado.

Risco de migration: variável.

## 6. Dependências entre os prompts

| Prompt | Depende de | Motivo |
|---|---|---|
| 01 | Nenhum | Corrige autorização básica imediatamente |
| 02 | 01 recomendado | Reutiliza roles/permissões corretas no refresh |
| 03 | 02 recomendado | Cookies ficam melhores com rotação robusta |
| 04 | Nenhum | Pode ser implementado independentemente |
| 05 | 01 e 02 recomendados | Invalidação deve conversar com sessão/refresh |
| 06 | Nenhum | Regra de negócio isolada |
| 07 | Nenhum | Mídia isolada, mas afeta playback |
| 08 | Nenhum | Operacional |
| 09 | Nenhum | Validação transversal |
| 10 | 01 recomendado | Policies devem considerar roles ativas |
| 11 | 10 recomendado | Consultas de acesso devem estar semânticas |
| 12 | Depende do item | Mistura regras funcionais e contratos |

## 7. Migrations previstas

| Correção | Migration provável? | Observação |
|---|---:|---|
| Roles ativas em queries | Não | Apenas filtro |
| Refresh token atômico/família | Sim | Pode exigir `FamilyId`, `ParentTokenId`, `RevokedReason` |
| Cookie HttpOnly | Não | Ajuste de controller/response |
| Rate limiting | Não | Configuração/middleware |
| TokenVersion/SecurityStamp | Sim | Coluna em usuários |
| Progresso controlado pelo servidor | Talvez | Depende de sessão/eventos |
| URLs assinadas | Talvez | Depende do modelo atual de vídeo |
| Docker seguro | Não | Compose/Dockerfile/docs |
| Validações de payload | Não | Aplicação |
| Paginação/N+1 | Talvez | Pode exigir índices |
| Health interno | Não | Middleware/configuração |

## 8. Checklist obrigatório por correção

Cada prompt de implementação deve exigir:

- diagnóstico inicial;
- arquivos lidos;
- arquivos criados/alterados/removidos;
- justificativa de qualquer alteração arquitetural;
- testes unitários;
- testes de integração HTTP quando aplicável;
- validação de OpenAPI/Postman quando contrato mudar;
- `dotnet restore`;
- `dotnet build`;
- `dotnet test`;
- `dotnet list package --vulnerable --include-transitive`;
- `git status`;
- `git status --ignored`;
- `git diff --stat`;
- commit somente se tudo passar.

## 9. Critérios para considerar pronto para produção

Antes de produção, devem estar resolvidos no mínimo:

- H-01;
- H-02;
- H-03;
- H-04;
- H-05;
- H-06;
- H-07;
- M-01;
- M-02;
- M-03;
- M-04;
- M-07;
- M-08;
- M-09;
- I-01;
- I-10.

Além disso:

- secrets reais fora do repositório;
- migrations aplicadas por processo controlado;
- seed desabilitado;
- CORS restrito;
- HTTPS/reverse proxy configurado;
- logs/monitoramento externo definidos;
- health detalhado restrito;
- backup e rollback documentados.

## 10. Convenção de nomes sugerida

Para próximas branches/commits:

```text
security/active-roles-auth
security/atomic-refresh-token
security/rate-limit-auth
security/http-only-refresh-cookie
security/token-version
security/server-side-progress
security/signed-video-playback
security/docker-safe-defaults
security/request-validation
security/access-policy-split
security/pagination-query-hardening
```

Commits sugeridos:

```text
fix: ignore inactive roles in authorization
feat: rotate refresh tokens atomically
feat: add auth rate limiting
feat: store refresh token in http-only cookie
feat: invalidate tokens with security stamp
fix: calculate lesson completion server-side
feat: generate signed video playback urls
chore: harden docker defaults
feat: add request validation limits
fix: split access management policies
perf: optimize available courses query
```
