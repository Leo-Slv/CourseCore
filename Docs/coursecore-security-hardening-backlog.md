# CourseCore — Backlog técnico de hardening de segurança

> Destino sugerido no repositório: `Docs/security-hardening-backlog.md`.

## 1. Objetivo

Este backlog detalha os itens de correção de segurança agrupados por blocos de implementação. Ele foi feito para servir como fonte para prompts futuros do Codex.

Não implemente todos os itens de uma vez. Use os grupos abaixo para criar prompts incrementais.

## 2. Regras gerais para todos os itens

Todo item deve respeitar:

- Clean Architecture / DDD modular;
- `Domain` sem dependência de EF Core/ASP.NET;
- `Application` sem acesso direto ao `DbContext`;
- `Infrastructure` responsável por EF Core, cookies, segurança técnica e persistência;
- `Presentation` responsável por controllers, requests, responses e cookies HTTP;
- `UnitOfWork` como centralizador de persistência;
- testes unitários e integração HTTP;
- sem secrets versionados;
- sem seed real;
- sem `database update` sem autorização;
- sem migration automática no startup.

---

# Grupo A — Autorização e roles ativas

## SEC-A01 — Ignorar roles inativas em autenticação

**Origem:** H-01  
**Severidade:** Alta  
**Bloqueia produção:** Sim

### Problema

Roles inativas continuam sendo carregadas no JWT como role claims. Isso permite que uma role `Admin` desativada continue autorizando endpoints administrativos.

### Comportamento esperado

- `FindByUserIdAsync` deve retornar somente roles ativas.
- `FindPermissionKeysByUserIdAsync` já deve continuar filtrando role ativa.
- Login e refresh não devem emitir role claim de role inativa.
- Usuário vinculado apenas a role inativa deve ficar sem autorização daquela role.

### Arquivos prováveis

```text
Modules/Access/Infrastructure/Persistence/Repositories/EfRoleRepository.cs
Modules/Auth/Application/UseCases/LoginUseCase.cs
Modules/Auth/Application/UseCases/RefreshTokenUseCase.cs
Tests/CourseCore.Api.Tests/
```

### Testes mínimos

- Login de usuário com role `Admin` inativa não emite `Admin`.
- Usuário com role `Admin` inativa recebe 403 em endpoint `ManageUsers`.
- Refresh de usuário com role inativa não reemite role inativa.

### Critério de aceite

- Todas as roles inativas são ignoradas na emissão de claims.
- Build/test passam.
- Nenhuma migration criada.

## SEC-A02 — Ignorar roles inativas no acesso a curso

**Origem:** H-01  
**Severidade:** Alta  
**Bloqueia produção:** Sim

### Problema

O acesso a cursos por `RoleAreaAccess` pode considerar roles carregadas sem validar `Role.Active`.

### Comportamento esperado

- `CourseAccessService` deve considerar somente roles ativas.
- Acessos concedidos a roles inativas não devem liberar curso.
- Acesso direto por usuário continua funcionando.

### Arquivos prováveis

```text
Modules/Access/Application/Services/CourseAccessService.cs
Modules/Access/Domain/Repositories/IRoleRepository.cs
Modules/Access/Infrastructure/Persistence/Repositories/EfRoleRepository.cs
Tests/CourseCore.Api.Tests/
```

### Testes mínimos

- Role ativa com acesso libera curso.
- Role inativa com acesso não libera curso.
- UserAreaAccess direto libera curso independentemente de role.

---

# Grupo B — Sessão, refresh token e cookies

## SEC-B01 — Rotação atômica de refresh token

**Origem:** H-02  
**Severidade:** Alta  
**Bloqueia produção:** Sim

### Problema

O refresh token é lido e validado antes da transação de revogação. Duas requisições simultâneas podem reutilizar o mesmo token antes da revogação ser persistida.

### Comportamento esperado

A rotação deve ser atômica:

- somente uma requisição consegue revogar o token antigo;
- se zero linhas forem afetadas, tratar como replay ou token inválido;
- não gerar dois refresh tokens sucessores válidos.

### Estratégias aceitáveis

1. `UPDATE` condicional no repositório.
2. Concorrência otimista com coluna de versão.
3. Lock pessimista no banco.
4. Transação serializável, se justificada.

### Arquivos prováveis

```text
Modules/Auth/Application/UseCases/RefreshTokenUseCase.cs
Modules/Auth/Domain/Repositories/IRefreshTokenRepository.cs
Modules/Auth/Infrastructure/Persistence/Repositories/EfRefreshTokenRepository.cs
Modules/Auth/Domain/Entities/RefreshToken.cs
Shared/Infrastructure/Persistence/Migrations/
Tests/CourseCore.Api.Tests/
```

### Testes mínimos

- Duas chamadas concorrentes com o mesmo refresh token: uma passa, outra falha.
- Token antigo fica revogado.
- Apenas um sucessor válido permanece.
- Replay gera auditoria segura.

## SEC-B02 — Família de refresh tokens e detecção de replay

**Origem:** M-04  
**Severidade:** Média/Alta  
**Bloqueia produção:** Recomendado

### Comportamento esperado

- Refresh tokens possuem família.
- Token sucessor referencia token anterior.
- Reutilização de token revogado revoga a família.
- Logout revoga token atual.
- “Logout all sessions” revoga todos os tokens do usuário.

### Migration provável

Sim.

Campos possíveis:

```text
family_id
parent_token_id
revoked_reason
revoked_by_reuse_at
```

## SEC-B03 — Refresh token em cookie HttpOnly

**Origem:** M-03  
**Severidade:** Média  
**Bloqueia produção:** Sim para SPA/PWA pública

### Decisão recomendada

```text
Access token: body e memória no frontend.
Refresh token: cookie HttpOnly, Secure, SameSite.
```

### Comportamento esperado

- Login seta cookie `refresh_token`.
- Refresh lê cookie por padrão.
- Refresh por body pode continuar como fallback para Postman/mobile.
- Logout limpa cookie.
- Cookie tem `HttpOnly=true`.
- Em produção, `Secure=true`.
- `SameSite=Lax` ou `Strict` quando same-site.
- `SameSite=None; Secure` apenas se front/API forem cross-site.

### Arquivos prováveis

```text
Modules/Auth/Presentation/Controllers/AuthController.cs
Modules/Auth/Presentation/Requests/
Modules/Auth/Presentation/Responses/
Modules/Auth/Application/UseCases/LoginUseCase.cs
Modules/Auth/Application/UseCases/RefreshTokenUseCase.cs
Docs/
Postman/
```

### Testes mínimos

- Login retorna access token e seta cookie HttpOnly.
- Refresh funciona lendo cookie.
- Refresh ainda funciona com body, se compatibilidade for mantida.
- Logout limpa cookie.

---

# Grupo C — Proteção contra brute force, enumeração e DoS

## SEC-C01 — Rate limiting em login e refresh

**Origem:** H-03  
**Severidade:** Alta  
**Bloqueia produção:** Sim

### Comportamento esperado

- `POST /api/auth/login` limitado por IP e e-mail normalizado.
- `POST /api/auth/refresh-token` limitado por IP e fingerprint/hash parcial seguro do token, se possível.
- Endpoints autenticados têm política geral por usuário/IP.
- Respostas de rate limit usam 429.
- Logs não incluem senha/token.

### Arquivos prováveis

```text
Program.cs
Modules/Auth/Presentation/Controllers/AuthController.cs
Shared/Presentation/
Tests/CourseCore.Api.Tests/
Docs/
```

### Testes mínimos

- Requisições acima do limite retornam 429.
- Login válido abaixo do limite continua funcionando.
- Refresh válido abaixo do limite continua funcionando.

## SEC-C02 — BCrypt fictício para usuário inexistente

**Origem:** M-02  
**Severidade:** Média  
**Bloqueia produção:** Recomendado

### Problema

Login retorna antes de executar BCrypt quando o usuário não existe. Isso permite enumeração por tempo.

### Comportamento esperado

- Quando e-mail não existir, executar verificação contra hash BCrypt fictício.
- Mensagem continua genérica.
- Logs continuam seguros.

### Arquivos prováveis

```text
Modules/Auth/Application/UseCases/LoginUseCase.cs
Modules/Auth/Application/Contracts/IPasswordHasher.cs
Modules/Auth/Infrastructure/Security/
Tests/CourseCore.Api.Tests/
```

## SEC-C03 — Política forte de senha

**Origem:** M-01  
**Severidade:** Média  
**Bloqueia produção:** Sim para aplicação pública

### Comportamento esperado

- Senha mínima de 12 caracteres.
- Máximo definido para evitar abuso.
- Não aceitar senha vazia/fraca.
- Validação centralizada.
- Mensagens públicas seguras.

### Arquivos prováveis

```text
Modules/Users/Application/UseCases/CreateUserUseCase.cs
Modules/Auth/Application/
Shared/Application/
Tests/CourseCore.Api.Tests/
```

---

# Grupo D — Invalidação de tokens e mudança de privilégios

## SEC-D01 — TokenVersion/SecurityStamp

**Origem:** H-05  
**Severidade:** Alta  
**Bloqueia produção:** Sim

### Problema

JWT emitido continua válido após desativar usuário ou alterar privilégios.

### Comportamento esperado

- Usuário possui `TokenVersion` ou `SecurityStamp`.
- JWT inclui a versão.
- Endpoints sensíveis validam versão atual.
- Alterações críticas incrementam versão.
- Refresh tokens são revogados quando a versão muda.

### Eventos que devem invalidar sessão

- desativação de usuário;
- troca de senha;
- remoção/adicionar role;
- alteração crítica de permissão;
- suspeita de comprometimento;
- logout all sessions.

### Migration provável

Sim.

### Testes mínimos

- Usuário desativado perde acesso imediatamente.
- Token emitido antes de alteração de role não autoriza mais.
- Refresh token antigo é rejeitado após incremento da versão.

---

# Grupo E — Integridade de progresso e regras de curso

## SEC-E01 — Conclusão de aula calculada pelo servidor

**Origem:** H-04  
**Severidade:** Alta  
**Bloqueia produção:** Sim se progresso gerar certificado, compliance ou benefício

### Problema

Cliente controla `markAsCompleted`.

### Comportamento esperado

- Cliente envia apenas progresso assistido.
- Servidor calcula conclusão.
- `WatchedSeconds` é monotônico.
- `WatchedSeconds` não passa da duração do vídeo.
- Aula conclui somente com percentual mínimo.
- Curso conclui somente quando todas as aulas realmente elegíveis foram concluídas.

### Arquivos prováveis

```text
Modules/Progress/Application/UseCases/RegisterLessonProgressUseCase.cs
Modules/Progress/Domain/Entities/UserLessonProgress.cs
Modules/Media/Domain/Entities/Video.cs
Modules/Courses/Domain/Entities/Lesson.cs
Tests/CourseCore.Api.Tests/
Postman/
Docs/
```

## SEC-E02 — Publicação de curso exige prontidão

**Origem:** I-07  
**Severidade:** Média  
**Bloqueia produção:** Recomendado

### Comportamento esperado

Curso só pode ser publicado se:

- tem ao menos uma área ativa;
- tem ao menos um módulo ativo;
- cada módulo obrigatório tem aula;
- aulas obrigatórias possuem vídeo pronto;
- dados mínimos preenchidos.

### Arquivos prováveis

```text
Modules/Courses/Application/UseCases/PublishCourseUseCase.cs
Modules/Courses/Domain/Entities/Course.cs
Tests/CourseCore.Api.Tests/
```

## SEC-E03 — FreePreview funcional

**Origem:** I-03  
**Severidade:** Média

### Comportamento esperado

- Aula com `FreePreview=true` permite playback sem acesso integral ao curso.
- Acesso gratuito é restrito somente à aula preview.
- Progresso/certificação continuam exigindo regra definida.

---

# Grupo F — Vídeo, storage e playback

## SEC-F01 — URL assinada e temporária para vídeo

**Origem:** H-06  
**Severidade:** Alta  
**Bloqueia produção:** Sim se vídeos forem privados

### Problema

Admin pode informar `PlaybackUrl`, e a API devolve a URL armazenada.

### Comportamento esperado

- Backend gera URL temporária no momento do playback.
- URL expira em poucos minutos.
- URL é gerada a partir de `StorageProvider` e `StorageKey`.
- API valida domínio/provedor.
- `PlaybackUrl` arbitrária não deve marcar vídeo como pronto.
- Não persistir URL pública longa como fonte de autorização.

### Arquivos prováveis

```text
Modules/Media/Application/UseCases/CreateVideoUseCase.cs
Modules/Media/Application/UseCases/RequestVideoPlaybackUseCase.cs
Modules/Media/Application/Contracts/IVideoStorageService.cs
Modules/Media/Infrastructure/
Tests/CourseCore.Api.Tests/
Docs/
```

### Testes mínimos

- CreateVideo não aceita playback URL arbitrária.
- Playback gera URL temporária via storage service.
- Usuário sem acesso não recebe URL.
- URL não é gravada em audit/log.

---

# Grupo G — Validação, limites e exceções

## SEC-G01 — Limites de payload e strings

**Origem:** M-07  
**Severidade:** Média  
**Bloqueia produção:** Recomendado

### Comportamento esperado

- Limites máximos para:
  - nome;
  - título;
  - slug;
  - descrição;
  - URL;
  - storage key;
  - quantidade de módulos;
  - quantidade de aulas;
  - quantidade de áreas;
  - tamanho de body.
- Erros retornam 400 padronizado.
- Limites estão alinhados com banco/domínio.

## SEC-G02 — Tratamento seguro de InvalidOperationException

**Origem:** M-08  
**Severidade:** Média

### Comportamento esperado

- `InvalidOperationException` desconhecida vira 500 em produção.
- Apenas exceções conhecidas de validação viram 400.
- Mensagens internas ficam só nos logs.
- `ApiErrorResponse` mantém `traceId` e `correlationId`.

---

# Grupo H — Access policies, grants e contratos

## SEC-H01 — Separar policies de acesso

**Origem:** M-09  
**Severidade:** Média/Alta

### Problema

`ManageAccess` autoriza ações diferentes com permissões amplas.

### Comportamento esperado

Criar policies:

```text
ManageUserAreaAccess
ManageRoleAreaAccess
CheckOwnCourseAccess
CheckUserCourseAccess
```

ou nomes equivalentes.

### Testes mínimos

- `areas.manage` não concede role-area se isso exigir `roles.manage`.
- `roles.manage` não concede user-area se isso exigir `areas.manage`.
- Admin continua fallback.

## SEC-H02 — Grants idempotentes

**Origem:** I-09  
**Severidade:** Média

### Comportamento esperado

- Repetir concessão existente não gera 500.
- Opções aceitáveis:
  - atualizar concessão existente;
  - retornar 200 com existente;
  - retornar 409 controlado.

---

# Grupo I — Paginação e consultas eficientes

## SEC-I01 — Paginar usuários

**Origem:** M-06  
**Severidade:** Média

### Comportamento esperado

```http
GET /api/users?page=1&pageSize=50
```

- limite máximo de `pageSize`;
- metadata de paginação;
- testes para limite e paginação.

## SEC-I02 — Otimizar cursos disponíveis

**Origem:** M-05  
**Severidade:** Média

### Comportamento esperado

- consulta única ou número controlado de consultas;
- considera:
  - usuário ativo;
  - curso publicado;
  - áreas ativas;
  - acesso direto válido;
  - role ativa;
  - acesso por role válido.
- adicionar paginação/cache se necessário.

---

# Grupo J — Docker, health e operação

## SEC-J01 — Docker sem defaults inseguros

**Origem:** H-07  
**Severidade:** Alta  
**Bloqueia produção:** Sim se ambiente for exposto

### Comportamento esperado

- remover default de `POSTGRES_PASSWORD=CHANGE_ME`;
- remover default de `Jwt__SecretKey=CHANGE_ME_USE_A_LONG_RANDOM_SECRET`;
- API falha se secret não for informado;
- `ASPNETCORE_ENVIRONMENT` não deve assumir `Development` em contexto não local;
- separar compose local de compose produção;
- container da API roda como usuário não-root;
- Postgres não fica exposto por padrão em perfil de produção.

## SEC-J02 — Health checks públicos mínimos

**Origem:** I-10  
**Severidade:** Baixa/Média

### Comportamento esperado

- `/health/live` público e mínimo;
- `/health/ready` e `/health` restritos por rede, auth, ou não expostos externamente;
- resposta externa não inclui nomes internos/tempos de componentes.

---

# Grupo K — Contratos e comportamento funcional

## SEC-K01 — UpdateUser não deve desativar usuário por omissão

**Origem:** I-01  
**Severidade:** Média/Alta

### Comportamento esperado

- `Active` deve ser `bool?`, PATCH, ou propriedade obrigatória validada explicitamente.
- Não permitir auto-desativação acidental.
- Não permitir desativar último admin.
- Não permitir remover último usuário com `ManageUsers`.

## SEC-K02 — E-mail verificado

**Origem:** I-02  
**Severidade:** Variável

### Decisão necessária

Escolher uma:

1. Login exige `EmailVerifiedAt`.
2. Campo é apenas informativo e deve ser documentado.
3. Remover ou adiar fluxo de verificação.

## SEC-K03 — Progresso por GET

**Origem:** I-06  
**Severidade:** Baixa/Média

### Comportamento esperado

Trocar consulta de progresso para:

```http
GET /api/progress/courses/{courseId}
```

Mantendo compatibilidade temporária com o POST atual, se necessário.

## SEC-K04 — Manutenção de módulos e aulas

**Origem:** I-08  
**Severidade:** Produto/Manutenção

### Comportamento esperado

Adicionar endpoints administrativos para:

- criar módulo;
- atualizar módulo;
- remover módulo;
- criar aula;
- atualizar aula;
- remover aula;
- reordenar módulos/aulas.

---

# Matriz resumida de aceite por grupo

| Grupo | Build/test | Integração HTTP | Migration | Postman | Docs |
|---|---:|---:|---:|---:|---:|
| A | Sim | Sim | Não | Não obrigatório | Sim |
| B | Sim | Sim | Provável | Sim | Sim |
| C | Sim | Sim | Não | Opcional | Sim |
| D | Sim | Sim | Sim | Não obrigatório | Sim |
| E | Sim | Sim | Talvez | Sim | Sim |
| F | Sim | Sim | Talvez | Sim | Sim |
| G | Sim | Sim | Não | Opcional | Sim |
| H | Sim | Sim | Não | Sim se contrato mudar | Sim |
| I | Sim | Sim | Talvez | Sim se contrato mudar | Sim |
| J | Sim | Config/Smoke | Não | Não | Sim |
| K | Sim | Sim | Variável | Sim se contrato mudar | Sim |

## Como usar este backlog nos próximos prompts

Para cada prompt do Codex:

1. Escolha um único grupo ou subgrupo.
2. Copie o problema, comportamento esperado, arquivos prováveis e testes.
3. Adicione as restrições globais.
4. Exija diagnóstico inicial.
5. Exija relatório final.
6. Exija commit somente após validação.

Modelo de título:

```text
Security Hardening XX — <nome do grupo>
```

Exemplo:

```text
Security Hardening 01 — Filtrar roles inativas em autenticação e acesso a cursos
```
