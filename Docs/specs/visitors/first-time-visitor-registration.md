# Spec — Cadastro de visitante de primeira vez

**Status:** Approved
**Aprovada em:** 2026-10-01 (decisões §11 fechadas com as opções recomendadas)
**Design:** `Docs/design/mockups/Primeira Vez.html` no repo do frontend (`plataforma-vdg`) — artboards `1a` (desktop 1280) e `1b` (mobile 390)

## 1. Objetivo

Dar suporte de backend à tela "Primeira vez na Viver da Graça": um formulário público em que uma pessoa visitando a igreja pela primeira vez deixa nome, telefone, e-mail e (opcionalmente) endereço, para que a equipe de recepção entre em contato nos dias seguintes.

**Isto não é login nem registro de conta.** O visitante não ganha usuário, senha, sessão, role, nem acesso a curso. É exclusivamente **coleta de dados de contato** — um "lead" de recepção — que fica disponível para a equipe interna consultar.

## 2. Contexto

### 2.1 O que o design mostra

- Uma única tela, pública, em duas variações responsivas (desktop/mobile) com o mesmo conteúdo.
- Campos:
  | Campo | Obrigatório | Observação do design |
  |---|---|---|
  | Nome completo | sim | validação do front: `trim().length > 1` |
  | Telefone | sim | máscara `(00) 00000-0000`; validação do front: ≥ 10 dígitos (máx. 11) |
  | E-mail | sim | validação do front: formato `x@y.z` |
  | Endereço | não | texto livre: "Rua, número, bairro, cidade" |
- Erro único de formulário: "Preencha nome, telefone e um e-mail válido."
- Estado de sucesso: "Obrigado, {primeiro nome}. Recebemos seus dados. Nossa equipe vai falar com você pelo telefone {telefone} em breve." — montado **no front** com o que ele mesmo digitou; o backend não precisa devolver esses dados.
- Botão "Cadastrar outra pessoa": volta ao formulário vazio. Indica uso possível em **totem/tablet da recepção**, com várias pessoas cadastradas do mesmo dispositivo/rede em sequência.
- Aviso de privacidade: "Usamos apenas para entrar em contato." — não há checkbox de consentimento.
- Cabeçalho ("Viver da Graça", "Cultos aos domingos · 10h e 18h") é conteúdo estático de front; não envolve backend.
- **Não há** widget de CAPTCHA visível no design (ver §11, decisão 1).
- **Não há** tela administrativa no design para a recepção consultar os cadastros (ver §11, decisão 2).

### 2.2 O que já existe e é reaproveitado

- **Endpoint público com rate limit por IP**: padrão de [RateLimiterExtensions.cs](../../../Shared/Presentation/RateLimiting/RateLimiterExtensions.cs) + [RateLimitOptions.cs](../../../Shared/Presentation/RateLimiting/RateLimitOptions.cs) (`[AllowAnonymous]` + `[EnableRateLimiting]`, como `POST /api/auth/register`).
- **CAPTCHA**: [ICaptchaVerificationService](../../../Modules/Auth/Application/Contracts/ICaptchaVerificationService.cs) com implementação Turnstile, já usado em registro e "esqueci a senha", já com fake nos testes.
- **Validação de e-mail**: value object [Email](../../../Shared/Domain/ValueObjects/Email.cs) em `Shared/Domain`.
- **Listagem paginada administrativa**: padrão de `ListUsersUseCase` (`PagedResult<T>`, `PaginationLimits`, busca textual).
- **Autorização por permissão**: `AddPermissionPolicy` em `AuthDependencyInjection` + permissão seedada em `CourseCoreDatabaseSeeder`.
- **Auditoria**: `IAuditLogService` / `AuditLogActionNames`.
- **Módulo pequeno de referência**: `Modules/Testimonials` (entidade simples, submissão + listagem administrativa) é o modelo estrutural mais próximo.

### 2.3 O que não existe hoje

- Nenhum conceito de visitante/contato/lead no domínio. Precisa de **módulo novo** e **migration de schema** (tabela nova).
- Nenhuma permissão que represente "equipe de recepção".

## 3. Comportamento esperado

### 3.1 Cadastro público

| Endpoint | Auth | Sucesso |
|---|---|---|
| `POST /api/visitors` | anônimo, rate-limited por IP | `201 Created` |

Corpo: `name`, `phone`, `email`, `address` (opcional), `captchaToken` (condicionado à decisão §11.1).

Resposta: apenas `id` e `submittedAt`. **Nenhum dado pessoal é ecoado** na resposta — o front já os tem.

### 3.2 Consulta pela equipe

| Endpoint | Auth | Sucesso |
|---|---|---|
| `GET /api/visitors?page=&pageSize=&search=` | permissão `visitors.read` | `200` com página de cadastros, mais recentes primeiro |

Cada item: `id`, `name`, `phone`, `email`, `address`, `submittedAt`. `search` busca por nome, e-mail ou telefone (dígitos).

## 4. Regras de negócio

1. Cadastro de visitante **não cria** `User`, não autentica, não envia e-mail e não concede nenhum acesso. É um registro independente.
2. Nome é obrigatório, com pelo menos 2 caracteres após `trim`, máximo 200 (mesmo limite de nome de usuário).
3. Telefone é obrigatório e é armazenado **normalizado em dígitos** (sem máscara). Após remover não-dígitos, deve ter 10 ou 11 dígitos (DDD + número fixo/celular brasileiro), conforme a máscara do design.
4. E-mail é obrigatório e segue as mesmas regras do value object `Email` (normalizado em minúsculas), máximo 320.
5. Endereço é opcional, texto livre, máximo 300; string vazia/só espaços vira ausente.
6. **Duplicidade é permitida.** O mesmo e-mail/telefone pode ser enviado mais de uma vez (a pessoa pode voltar, um familiar pode compartilhar contato). Nenhum `409`. A deduplicação, se desejada, é trabalho da equipe na leitura.
7. A existência ou não de um cadastro anterior **nunca** é revelada ao chamador anônimo (consequência da regra 6 — resposta é sempre a mesma).
8. Listagem e qualquer leitura de dados de visitante exigem a permissão `visitors.read`; Admin a recebe via seed, como as demais permissões.
9. A auditoria do cadastro registra só o `id` do visitante — **sem nome, e-mail, telefone ou endereço** nos metadados (dados pessoais de pessoa sem conta não devem se espalhar para o log de auditoria).
10. O rate limit por IP precisa tolerar o uso em totem da recepção (várias pessoas, mesmo IP, em poucos minutos) e é configurável via `RateLimiting:VisitorRegistration`.

## 5. Pré-condições

- Cadastro: nenhuma autenticação; CAPTCHA válido se a decisão §11.1 for "sim".
- Listagem: usuário autenticado com permissão `visitors.read` (ou Admin).

## 6. Fluxo principal

### 6.1 Cadastro

1. (Se §11.1 = sim) O sistema verifica o CAPTCHA no servidor; se inválido, para aqui.
2. O sistema valida nome, telefone, e-mail e endereço (regras 2–5).
3. O sistema cria o registro de visitante com data de envio.
4. O sistema registra `VisitorRegistered` na auditoria, só com o `id`.
5. Responde `201` com `id` e `submittedAt`.

### 6.2 Listagem

1. Usuário da equipe pede uma página, opcionalmente com `search`.
2. O sistema devolve os cadastros ordenados do mais recente para o mais antigo, paginados.

## 7. Cenários de erro

| Cenário | Fluxo | HTTP |
|---|---|---|
| CAPTCHA ausente ou inválido (se exigido) | Cadastro | `400` |
| Nome ausente/curto/longo demais | Cadastro | `400` |
| Telefone ausente ou fora de 10–11 dígitos | Cadastro | `400` |
| E-mail ausente ou inválido | Cadastro | `400` |
| Endereço acima do limite | Cadastro | `400` |
| Excesso de envios do mesmo IP | Cadastro | `429` |
| Não autenticado | Listagem | `401` |
| Sem permissão `visitors.read` | Listagem | `403` |
| `page`/`pageSize` inválidos | Listagem | `400` |

Erros de validação usam o formato `ApiErrorResponse` existente. O front exibe uma mensagem única ("Preencha nome, telefone e um e-mail válido."), então o backend não precisa de mensagens por campo além das que já produz.

## 8. Casos de borda

- **Telefone enviado com máscara** (`(11) 98765-4321`) ou sem (`11987654321`): ambos aceitos e gravados como `11987654321`.
- **Telefone com `+55`**: 12–13 dígitos → rejeitado pela regra 3. O design não prevê DDI; se for necessário aceitar, é mudança de regra (fora desta spec).
- **Mesmo totem, várias pessoas seguidas**: caso normal (regra 10); não deve esbarrar no rate limit com uso humano.
- **Envio duplicado por clique duplo**: gera dois registros (regra 6). Mitigação de clique duplo é responsabilidade do front (desabilitar botão durante o envio).
- **Usuário logado na plataforma acessa a tela**: o endpoint é anônimo; o cadastro não é vinculado ao usuário logado.
- **Nome com uma só palavra**: aceito; "primeiro nome" da tela de sucesso é derivado no front.

## 9. Critérios de aceite

- [x] Uma pessoa sem conta consegue enviar nome, telefone e e-mail e recebe `201` com `id` e `submittedAt`, sem dados pessoais na resposta.
- [x] Endereço é opcional; ausente/vazio é aceito e gravado como nulo.
- [x] Telefone mascarado ou só dígitos é aceito e persistido apenas com dígitos; menos de 10 ou mais de 11 dígitos → `400`.
- [x] E-mail inválido → `400`; e-mail válido é persistido normalizado.
- [x] Nome com menos de 2 caracteres úteis → `400`.
- [x] Envio repetido com os mesmos dados gera um segundo registro, sem `409`.
- [x] Nenhum `User`, sessão ou cookie é criado pelo cadastro.
- [x] (Se §11.1 = sim) CAPTCHA inválido → `400` e nada é gravado.
- [x] Rate limit por IP aplicado e configurável; excesso → `429`.
- [x] `GET /api/visitors` exige `visitors.read`: anônimo → `401`, autenticado sem permissão → `403`, Admin → `200`.
- [x] Listagem é paginada, ordenada do mais recente, e `search` encontra por nome, e-mail ou telefone.
- [x] O registro de auditoria `VisitorRegistered` não contém nome, e-mail, telefone nem endereço.
- [x] `dotnet build` e `dotnet test` passam sem regressão; a migration da tabela nova é criada e revisada antes do commit.

## 10. Fora de escopo

- Login, criação de conta, confirmação de e-mail ou qualquer envio de e-mail/SMS ao visitante.
- Notificação automática à equipe de recepção (e-mail, WhatsApp, webhook) a cada novo cadastro.
- Fluxo de acompanhamento (status "contatado", responsável, anotações) — ver §11.2.
- Edição/exclusão de cadastro pela equipe e política de retenção/anonimização (LGPD) — registrada como pendência, não implementada aqui.
- Exportação CSV.
- Aceitar telefone internacional.
- Conteúdo do cabeçalho (horário dos cultos) vindo do backend.

## 11. Decisões

### Resolvidas (2026-10-01 — opções recomendadas, aceitas ao aprovar a implementação do plano)

1. ✅ **CAPTCHA no cadastro de visitante?** → (a). O design não mostra widget, mas o endpoint é anônimo e grava dados — é alvo óbvio de spam.
   - (a) **Recomendado:** sim, Turnstile em modo *managed/invisible* (sem mudança visual relevante), reaproveitando `ICaptchaVerificationService`.
   - (b) Não; só rate limit por IP.
2. ✅ **Como a equipe consulta os cadastros?** → (a). O design só cobre o lado público.
   - (a) **Recomendado:** apenas `GET /api/visitors` paginado (§3.2) agora; tela admin e status de acompanhamento numa spec futura.
   - (b) Incluir já marcação "contatado" (`POST /api/visitors/{id}/contacted`) e filtro por status.
   - (c) Nenhuma leitura via API agora (consulta direto no banco) — não recomendado.
3. ✅ **Quem pode ler os cadastros?** → `visitors.read`. Recomendado: permissão nova `visitors.read` (seedada, Admin recebe automaticamente), permitindo criar depois uma role "Recepção" sem dar acesso administrativo amplo.
4. ✅ **Limite de rate limit padrão** → 10/60 s. Recomendado: 10 envios / 60 s por IP (mais folgado que o registro de conta, por causa do uso em totem — regra 10).
