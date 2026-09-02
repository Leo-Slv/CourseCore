# Spec — Registro público, cursos gratuitos e catálogo com bloqueio

**Status:** Approved
**Aprovada em:** 2026-09-02

## 1. Objetivo

Permitir que o produto funcione como descrito pelo dono da plataforma (single-tenant: uma instância = um dono + seus clientes, confirmado em conversa anterior):

- Qualquer pessoa pode se registrar sozinha na aplicação, protegida por CAPTCHA e com confirmação de e-mail obrigatória antes de conseguir acessar qualquer conteúdo — sem depender de um admin criar a conta manualmente.
- A partir do registro (e da confirmação de e-mail), o usuário enxerga um catálogo com todas as Areas ativas e todos os Courses publicados — os que ele tem acesso (por grant existente ou por serem gratuitos) aparecem liberados; os que não tem aparecem listados, porém bloqueados (é aí que entraria pagamento, fora de escopo agora).
- Um Course tem um modelo de preço — hoje `Free` ou `Paid` — que decide se ele é liberado a qualquer usuário verificado ou só a quem tem grant. A mesma Area pode ter cursos de ambos os modelos misturados; a Area em si nunca é bloqueada, só o Course.

Isso substitui a ideia de convite discutida antes: o registro aberto passa a ter propósito real (ver o catálogo e os cursos gratuitos), sem exigir multi-tenancy nem sistema de convite.

## 2. Contexto

### 2.1 O que já existe e é reaproveitado

- **Emissão de sessão**: [LoginUseCase.cs](../../../Modules/Auth/Application/UseCases/LoginUseCase.cs) já monta o fluxo completo de autenticação (gera access token, refresh token, hash do refresh token, grava audit log) dentro de `IUnitOfWork`. [AuthController.cs](../../../Modules/Auth/Presentation/Controllers/AuthController.cs) já sabe colocar o refresh token em cookie `HttpOnly` e devolver `AuthResponse`. Registro deve produzir a mesma sessão no final, não só criar a conta.
- **Validação de cadastro**: [CreateUserUseCase.cs](../../../Modules/Users/Application/UseCases/CreateUserUseCase.cs) já valida nome, formato de e-mail, política de senha (`IPasswordPolicy`), unicidade de e-mail (`ExistsByEmailAsync`), e cria o usuário via `User.Create`. O mesmo conjunto de regras vale para registro público — a diferença é quem pode chamar, o que acontece depois (login automático, mas sem acesso a conteúdo até confirmar e-mail) e que role é atribuída (nenhuma, ver §4).
- **Rate limiting**: [RateLimiterExtensions.cs](../../../Shared/Presentation/RateLimiting/RateLimiterExtensions.cs) já tem policies por IP para `login`/`refresh`/`logout`, configuráveis via `RateLimitOptions`. Registro usa o mesmo padrão (§11, decisão 4 já resolvida — mesma janela do login).
- **Controle de acesso a curso**: [CourseAccessService.CanUserAccessCourseAsync](../../../Modules/Access/Application/Services/CourseAccessService.cs) já centraliza toda a decisão de "esse usuário pode ver esse curso" — é o único lugar que precisa aprender a nova regra de modelo de preço **e** a exigência de e-mail confirmado; nenhum outro use case decide acesso por conta própria (todos delegam pra esse serviço).
- **Campo já existente, nunca usado**: `User.EmailVerifiedAt` já existe no domínio, na tabela e no seed (o admin nasce com e-mail verificado) — só nunca é lido por nenhuma regra de autorização nem escrito por nenhum fluxo de usuário comum. Esta spec é a primeira a dar utilidade real a esse campo.
- **Catálogo publicado, já pronto e nunca usado**: `ICourseRepository.ListPublishedAsync()` ([EfCourseRepository.cs:69](../../../Modules/Courses/Infrastructure/Persistence/Repositories/EfCourseRepository.cs)) já existe, já carrega `CourseAreas` (então `Course.AreaIds` vem populado), e não é chamado por nenhum use case hoje — é exatamente a base de dados que o catálogo com bloqueio precisa, só falta a camada de Application/Presentation por cima.
- **Padrão de token descartável já existente**: o módulo Auth já tem um padrão completo de token opaco com hash e expiração para `RefreshToken` (gerador, hasher, repositório, rotação). O token de confirmação de e-mail (§3.4) segue o mesmo padrão arquitetural, como uma entidade própria — não é o mesmo token, mas é a mesma forma de resolver o problema já validada no projeto.

### 2.2 O que não existe hoje (gaps novos, confirmados por busca no código)

- Nenhum endpoint público de registro.
- Nenhum flag/enum de modelo de preço em `Course` — nem no domínio, nem na tabela `courses`. **Migration de schema necessária.**
- Nenhum endpoint que liste o catálogo completo (acessível + bloqueado) para um usuário comum.
- **Nenhuma infraestrutura de e-mail no projeto.** Busquei por `IEmailService`/SMTP/qualquer client de envio — zero resultado. Confirmar e-mail por link/código exige montar isso do zero: não é só "adicionar uma regra", é uma capacidade nova de infraestrutura.
- **Nenhuma integração de CAPTCHA no projeto.** Zero resultado para qualquer provedor. Também é capacidade nova, com uma decisão de fornecedor que só você pode tomar (§11).
- Nenhuma role diferente de `Admin` — mas ver §4, regra 1: registro público não precisa criar uma role nova, porque autorização aqui não passa por RBAC.

## 3. Comportamento esperado

### 3.1 Registro

| Endpoint | Sucesso |
|---|---|
| `POST /api/auth/register` (público, rate-limited) | `201`/`200` com o mesmo formato de `AuthResponse` do login; cookie de refresh token já setado |

Corpo de entrada: nome, e-mail, senha, resposta do CAPTCHA — mesmo formato de `CreateUserRequest` mais o token do CAPTCHA, sem `roleIds` (registro não atribui role). A conta nasce **sem e-mail confirmado**; a sessão retornada permite login, mas ainda não acesso a conteúdo (ver §3.4 e §4).

### 3.2 Modelo de preço do curso

`Course` ganha um modelo de preço, com dois valores hoje — gratuito ou pago —, alterável pelo dono via o fluxo administrativo de curso já existente (create/update), sujeito à mesma policy `ManageCourses` de hoje. É desenhado para crescer (ex.: distinguir compra única de assinatura, no futuro) sem precisar renomear o campo de novo — mas só os dois valores atuais são implementados agora; nenhum valor futuro é inventado nesta spec.

### 3.3 Catálogo com bloqueio

`GET /api/courses/available` evolui no lugar (não ganha um endpoint irmão): por padrão devolve todas as Areas ativas e todos os Courses publicados, cada um com um indicador de acesso; aceita um filtro opcional na URL para o front pedir só o que está liberado ou só o que está bloqueado, quando quiser uma visão recortada (ex.: uma tela "meus cursos" vs. uma tela "explorar catálogo").

- Areas: sempre visíveis quando ativas, nunca marcadas como bloqueadas.
- Courses: cada um com indicador dizendo se o usuário tem acesso (por grant de Area, por o curso ser gratuito, ou nenhum dos dois — bloqueado) e a qual Area pertence (decisão §11.2, opção (a): informação de Area embutida no item do curso, sem endpoint de Area dedicado para o cliente).
- Um Course sem acesso aparece com metadados de vitrine (título, descrição, thumbnail, Area) mas **não** com conteúdo interno — isso já é garantido pela regra existente de `GetCourseDetailsUseCase`, que barra com `403` quem não tem acesso.
- Catálogo é visível a qualquer usuário autenticado, **mesmo antes de confirmar e-mail** — é vitrine, não é acesso a conteúdo (ver regra 10). O que exige e-mail confirmado é entrar de fato num curso (detalhe, progresso, playback), não ver que ele existe.

### 3.4 Confirmação de e-mail

| Endpoint | Efeito |
|---|---|
| Confirmar e-mail (token recebido por e-mail) | Marca `EmailVerifiedAt`, libera acesso a conteúdo |
| Reenviar confirmação | Gera um novo token, invalida o anterior, envia novo e-mail |

O link de confirmação chega por e-mail; quem efetivamente marca o e-mail como confirmado é uma chamada que muta estado (não uma leitura), então não é modelada como `GET` — consistente com a spec de migração de GET já aplicada neste projeto (não expor mutação de estado atrás de um verbo de leitura). Cabe ao frontend decidir como o clique no link do e-mail dispara essa chamada.

## 4. Regras de negócio

1. Usuário criado por registro público não recebe nenhuma role. Toda autorização dele continua vindo de `UserAreaAccess`/`RoleAreaAccess` (grant explícito) ou do modelo de preço do curso — nunca de RBAC.
2. Registro segue as mesmas regras de validação de criação de usuário hoje (nome, e-mail válido e único, senha dentro da política) mais a validação do CAPTCHA.
3. O CAPTCHA é verificado no servidor antes de qualquer outra validação prosseguir; falha de CAPTCHA impede a criação da conta.
4. Registro bem-sucedido autentica o usuário no mesmo request (mesmo mecanismo do login) — mas a conta nasce com e-mail não confirmado, e por isso ainda sem acesso a conteúdo (regra 10). O cliente não precisa logar separadamente, mas precisa confirmar o e-mail antes de ver um curso de verdade.
5. Um Course com modelo de preço `Free` é acessível a qualquer usuário autenticado, ativo e **com e-mail confirmado**, desde que o curso esteja publicado — rascunho gratuito continua invisível, igual qualquer outro rascunho hoje.
6. Um Course `Free` é acessível mesmo que não esteja vinculado a nenhuma Area. Hoje um curso sem nenhuma Area vinculada é sempre negado (regra existente); essa regra continua valendo para cursos `Paid`, mas não se aplica a `Free`.
7. Modelo de preço é uma propriedade do Course, não da Area — a mesma Area pode ter cursos `Free` e `Paid` ao mesmo tempo; isso é o caso normal, não um caso de borda.
8. Area nunca é bloqueada no catálogo. O bloqueio é sempre por Course.
9. O catálogo (listagem com indicador de acesso) exige usuário autenticado, mas **não** exige e-mail confirmado — é vitrine. Visitante sem conta não vê o catálogo; precisa registrar primeiro, mas não precisa confirmar e-mail só para navegar o catálogo.
10. Qualquer acesso a conteúdo de curso (detalhe, progresso, playback de vídeo) — seja o curso `Free` ou `Paid` com grant — exige e-mail confirmado, além das regras que já existem hoje (usuário ativo, curso publicado, grant ou gratuidade). Confirmação de e-mail é um pré-requisito único aplicado antes de qualquer outra regra de acesso, não uma regra separada por tipo de curso.
11. Reenviar confirmação invalida o token anterior — só o token mais recente enviado funciona.
12. Alterar o modelo de preço de um Course tem efeito imediato sobre quem consegue acessá-lo — mesma natureza do efeito já documentado para desativação de Area (spec de Area CRUD, regra 11): usuários sem grant próprio perdem acesso assim que o curso deixa de ser `Free`, sem aviso adicional.

## 5. Pré-condições

- Registro: CAPTCHA válido.
- Catálogo: usuário autenticado, ativo (e-mail confirmado **não** é exigido para ver o catálogo).
- Acesso a conteúdo de curso: usuário autenticado, ativo, **e-mail confirmado**.
- Marcar curso como `Free`/`Paid`: mesma pré-condição de qualquer edição de curso hoje (policy `ManageCourses`).
- Confirmar e-mail: token de confirmação válido e não expirado.

## 6. Fluxo principal

### 6.1 Registro

1. O sistema verifica o CAPTCHA no servidor; se inválido, a operação para aqui.
2. O sistema valida os dados recebidos (nome, e-mail, senha) com as mesmas regras já usadas na criação administrativa de usuário.
3. O sistema garante que o e-mail não está em uso.
4. A conta é criada, ativa, sem nenhuma role, com e-mail não confirmado.
5. O sistema envia um e-mail de confirmação com um token/link.
6. O sistema gera uma sessão para a conta recém-criada (mesmo mecanismo do login) e a devolve ao cliente, já com o cookie de refresh token setado.
7. A criação é registrada em auditoria, distinguível de uma criação administrativa.

### 6.2 Confirmação de e-mail

1. O cliente envia o token recebido por e-mail.
2. O sistema valida que o token existe, não expirou e não foi usado.
3. O sistema marca o e-mail do usuário como confirmado e invalida o token.

### 6.3 Acesso a curso

1. Um usuário autenticado pede acesso a um curso (detalhe, progresso, playback — qualquer fluxo que hoje passa por `CourseAccessService`).
2. O sistema confirma que o usuário está ativo e com e-mail confirmado; caso contrário, nega.
3. Se o curso está publicado e é `Free`, o acesso é liberado, sem checar grant de Area.
4. Caso contrário, a regra de hoje continua valendo (grant de Area por usuário ou por role).

### 6.4 Catálogo

1. O usuário autenticado (e-mail confirmado ou não) pede o catálogo, opcionalmente com um filtro de acesso.
2. O sistema lista todas as Areas ativas.
3. O sistema lista todos os Courses publicados, calcula se o usuário tem acesso a cada um (grant ou `Free`) e devolve a lista completa com essa marcação, aplicando o filtro pedido se houver — nunca omite os bloqueados por padrão, nunca bloqueia a Area.

## 7. Cenários de erro

| Cenário | Fluxo | HTTP |
|---|---|---|
| CAPTCHA ausente ou inválido | Registro | `400` |
| E-mail já cadastrado | Registro | `409` |
| Nome/e-mail/senha fora das regras de validação | Registro | `400` |
| Excesso de tentativas de registro (mesmo IP) | Registro | `429` |
| Token de confirmação inválido, expirado ou já usado | Confirmar e-mail | `400` |
| Reenvio de confirmação para e-mail já confirmado | Reenviar confirmação | `409` |
| Usuário não autenticado pedindo o catálogo | Catálogo | `401` |
| Usuário sem e-mail confirmado tentando acessar conteúdo de curso | Acesso a curso | `403` |
| Usuário inativo tentando acessar curso | Acesso a curso | `403` (regra já existente) |

## 8. Casos de borda

- **Usuário tem grant de Area E o curso também é `Free`**: acesso liberado de qualquer forma — os dois motivos não conflitam.
- **Curso `Free` despublicado**: some do catálogo como acessível e como listado, igual qualquer curso despublicado hoje.
- **Curso muda de `Free` para `Paid` com usuários já tendo acessado antes**: quem não tinha grant próprio perde acesso na próxima checagem (regra 12). Nenhum aviso é enviado.
- **Curso `Free` sem nenhuma Area vinculada**: continua acessível (regra 6); aparece no catálogo sem agrupamento de Area, ou agrupado como "sem área" — decisão de exibição na implementação, não de acesso.
- **Usuário confirma o e-mail depois de já ter navegado o catálogo (tudo bloqueado por falta de confirmação)**: a partir da confirmação, os cursos `Free`/com grant passam a abrir normalmente — não precisa de nenhuma ação adicional além de confirmar.
- **Usuário pede reenvio de confirmação repetidamente**: cada pedido invalida o token anterior (regra 11); vale considerar um rate limit próprio para esse endpoint também, mesmo motivo do registro (evitar abuso de envio de e-mail).
- **Registro com o mesmo nome de um usuário já existente, e-mail diferente**: permitido — nome não é único, só e-mail é.

## 9. Critérios de aceite

- [ ] Registro sem CAPTCHA válido é rejeitado antes de qualquer outra validação.
- [ ] Uma pessoa sem conta consegue se registrar (com CAPTCHA válido) e, no mesmo request, já recebe uma sessão válida.
- [ ] Usuário recém-registrado não consegue ver detalhe/progresso/playback de nenhum curso (nem `Free`) até confirmar o e-mail.
- [ ] Usuário recém-registrado, mesmo sem confirmar e-mail, consegue ver o catálogo (com tudo bloqueado, já que nada foi confirmado ainda).
- [ ] Confirmar e-mail com token válido libera imediatamente o acesso aos cursos `Free`/com grant, sem precisar de novo login.
- [ ] Reenviar confirmação invalida o token anterior.
- [ ] Um Course `Free` e publicado é acessível a qualquer usuário com e-mail confirmado, mesmo sem grant de Area.
- [ ] Um Course `Free`, mas despublicado, continua inacessível.
- [ ] Uma Area com um curso `Free` e um `Paid` mostra os dois no catálogo, com o `Free` liberado (para quem já confirmou e-mail) e o `Paid` bloqueado.
- [ ] Nenhuma Area aparece bloqueada no catálogo.
- [ ] O filtro opcional de acesso no catálogo funciona (só liberado / só bloqueado / tudo).
- [ ] Registro tem rate limiting por IP, mesmo padrão de login.
- [ ] `dotnet build` e `dotnet test` passam sem regressão; migrations de schema (modelo de preço do curso, tabela de token de confirmação de e-mail) são criadas e revisadas antes do commit.

## 10. Fora de escopo

- Qualquer forma de pagamento, checkout, assinatura ou cobrança — o bloqueio no catálogo é só visual/informativo, sem nenhum caminho de "clicar para comprar". O modelo de preço registra só a classificação (`Free`/`Paid`), não valor, moeda ou cobrança.
- Recuperação de senha / "esqueci minha senha" (gap identificado antes, permanece fora).
- Sistema de convite (descartado a favor deste modelo).
- Multi-tenancy (confirmado: modelo continua single-tenant).
- Vitrine pública para visitante sem conta (landing page de marketing é um site separado, fora da API; o catálogo desta spec exige login).
- Mudar a autorização de qualquer endpoint administrativo existente.
- CAPTCHA em login ou em qualquer outro endpoint além de registro (e, se fizer sentido, reenvio de confirmação) — não foi pedido para os demais.

## 11. Decisões

Decisões já resolvidas em conversa anterior:

1. ✅ `GET /api/courses/available` evolui no lugar, com filtro opcional via URL — não ganha endpoint irmão.
2. ✅ Area embutida no item do curso no catálogo — sem endpoint de Area dedicado ao cliente.
3. ✅ Modelo de preço é um enum extensível (`Free`/`Paid` agora), não um booleano simples.
4. ✅ Rate limiting de registro segue o mesmo padrão de login.
5. ✅ CAPTCHA + confirmação de e-mail obrigatória entram nesta etapa.

Decisões que a resolução acima abriu, agora também fechadas:

6. ✅ CAPTCHA: **Cloudflare Turnstile**.
7. ✅ E-mail transacional: **Resend**.
8. ✅ Confirmação de e-mail via `POST` disparado pelo frontend (o link no e-mail aponta para uma página do frontend, que chama a API — a API não responde diretamente ao clique).

### 11.1 Credenciais ainda não fornecidas

Turnstile e Resend ainda não têm chave/segredo fornecidos por você. Isso não bloqueia a implementação — segue o mesmo padrão já usado no projeto para segredo (JWT secret, senha do admin de seed, connection string): configuração externa via `appsettings`/variável de ambiente, com `IOptions<T>` para cada integração, chave ausente/vazia em desenvolvimento. O sistema fica pronto para receber as chaves reais — você as adiciona depois, sem precisar de mudança de código.

Implicações diretas para os critérios de aceite (§9) e para os testes:

- Testes automatizados (build, `dotnet test`, CI) **não podem depender de chamada real** ao Turnstile nem ao Resend — nenhuma chave real está disponível nesse ambiente. A verificação de CAPTCHA e o envio de e-mail precisam de uma abstração (`ICaptchaVerificationService`, algo como `IEmailSender`) substituível por uma implementação fake/stub nos testes, mesmo espírito de como os testes de integração já rodam com SQLite in-memory em vez de PostgreSQL real.
- Enquanto a chave do Turnstile não estiver configurada, o comportamento em desenvolvimento (não produção) deveria permitir seguir testando manualmente sem CAPTCHA real bloqueando — isso é uma decisão de implementação (ex.: pular verificação se a chave não estiver configurada fora de produção), não desta spec; sinalizo aqui só para não travar seu teste manual local antes de configurar as chaves.
- Validação de configuração obrigatória em produção (mesmo padrão de `ValidateProductionConfiguration()` já existente) deve incluir Turnstile e Resend — produção não deve subir sem essas chaves configuradas, já que nessa altura CAPTCHA e confirmação de e-mail são obrigatórios.
