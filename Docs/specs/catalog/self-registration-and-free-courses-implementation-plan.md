# Implementation Plan — Registro público, cursos gratuitos e catálogo com bloqueio

**Spec:** [Docs/specs/catalog/self-registration-and-free-courses.md](self-registration-and-free-courses.md) (Approved, 2026-09-02)
**Status:** Draft — aguardando aprovação antes de qualquer código.

Este documento é o **HOW**. É o maior plano desta série — toca três módulos (Auth, Courses, Access) mais Shared, e é a primeira feature que introduz duas capacidades de infraestrutura genuinamente novas (envio de e-mail, verificação de CAPTCHA) e duas migrations de schema.

## 1. Escopo técnico e ordem de implementação

1. `Shared`: abstração de envio de e-mail (Resend).
2. `Auth`: CAPTCHA (Turnstile), token de confirmação de e-mail, use cases de registro/confirmação/reenvio, controller, rate limiting, configuração.
3. `Courses`: modelo de preço do curso (enum), migration.
4. `Access`: `CourseAccessService` aprende as duas novas regras (e-mail confirmado como pré-requisito, curso `Free` libera sem grant) e ganha o método de catálogo.
5. `Courses` (Presentation): `GET /api/courses/available` evolui para devolver Areas + Courses com indicador de acesso e filtro opcional.
6. Validação de configuração de produção.
7. Testes (com fakes para Turnstile/Resend — nenhuma chamada real nos testes).
8. Documentação (Postman, `postman.md`, README, diagrama).

## 2. Shared — envio de e-mail (Resend)

Novo, não existe nada parecido hoje (`grep` confirmou zero infraestrutura de e-mail no projeto).

- `Shared/Application/Contracts/IEmailSender.cs` — `Task SendAsync(string to, string subject, string htmlBody, CancellationToken)`. Fica em `Shared` (não em Auth) porque é infraestrutura genuinamente transversal, no mesmo espírito de `IUnitOfWork`/`ICurrentUserService` — mesmo que só o fluxo de confirmação de e-mail o use por enquanto.
- `Shared/Infrastructure/Email/ResendEmailSender.cs` — implementação via `HttpClient` tipado, chamando a API REST do Resend.
- `Shared/Infrastructure/Email/ResendOptions.cs` — `ApiKey`, `FromAddress`, `FromName`. `ApiKey` vazio em desenvolvimento (mesmo padrão de `Jwt:SecretKey`/`Media:Playback:SigningSecret`).
- Registro em `Shared` (`AddSharedInfrastructure`, onde `ICurrentUserService` etc. já são registrados): `services.AddHttpClient<IEmailSender, ResendEmailSender>()` — primeiro uso de `AddHttpClient` no projeto (`grep` confirmou que não existe nenhum `HttpClient` de saída registrado hoje).
- Comportamento quando `ApiKey` está vazio fora de Production: logar um aviso e não lançar exceção — deixa o fluxo de registro funcionar localmente sem a chave real (mesmo raciocínio já combinado para o Turnstile, ver §3.1). Em Production, chave ausente é bloqueada por `ValidateProductionConfiguration` (§6).

## 3. Auth — CAPTCHA, confirmação de e-mail, registro

### 3.1 CAPTCHA (Turnstile)

- `Modules/Auth/Application/Contracts/ICaptchaVerificationService.cs` — `Task<bool> VerifyAsync(string captchaToken, CancellationToken)`.
- `Modules/Auth/Infrastructure/Security/TurnstileCaptchaVerificationService.cs` — `HttpClient` tipado chamando o endpoint `siteverify` do Turnstile com o `SecretKey` configurado.
- `Modules/Auth/Infrastructure/Security/TurnstileOptions.cs` — `SecretKey` (vazio em dev).
- Comportamento com `SecretKey` vazio fora de Production: `VerifyAsync` retorna `true` sem chamar a rede (log de aviso) — permite testar o registro manualmente antes de configurar a chave real. Em Production, é bloqueado por `ValidateProductionConfiguration` (§6), então esse atalho nunca se aplica lá.
- Registro em `AuthDependencyInjection`: `services.AddHttpClient<ICaptchaVerificationService, TurnstileCaptchaVerificationService>()`.

### 3.2 Token de confirmação de e-mail

Mesmo padrão arquitetural de `RefreshToken` ([RefreshToken.cs](../../../Modules/Auth/Domain/Entities/RefreshToken.cs), [IRefreshTokenRepository.cs](../../../Modules/Auth/Domain/Repositories/IRefreshTokenRepository.cs)), como uma entidade própria — reaproveitar a *forma*, não a mesma tabela nem a mesma interface de hash/geração (evita acoplar um refactor da rotação de refresh token, que já é testada e sensível, a esta feature).

- `Modules/Auth/Domain/Entities/EmailVerificationToken.cs` — `UserId`, `TokenHash`, `ExpiresAt`, `CreatedAt`, `ConsumedAt` (em vez de `RevokedAt`/`ReplacedByTokenHash` do refresh token — aqui não existe rotação, só consumo único).
- `Modules/Auth/Domain/Repositories/IEmailVerificationTokenRepository.cs` — `FindByTokenHashAsync`, `AddAsync`, `TryConsumeAsync` (mesmo espírito atômico do `TryRotateAsync`/`TryRevokeAsync` do refresh token, evitando race de uso duplo), `InvalidateActiveByUserIdAsync` (usado no reenvio, regra 11 da spec: reenviar invalida o token anterior).
- `Modules/Auth/Infrastructure/Persistence/{Models,Configurations,Mappers,Repositories}` — `EmailVerificationTokenPersistenceModel`, `EmailVerificationTokenConfiguration`, `EmailVerificationTokenMapper`, `EfEmailVerificationTokenRepository`, mesmo padrão de arquivo dos equivalentes de `RefreshToken`.
- `Modules/Auth/Application/Contracts/IEmailVerificationTokenHasher.cs` / `IEmailVerificationTokenGenerator.cs` + implementações `Sha256EmailVerificationTokenHasher`/`SecureEmailVerificationTokenGenerator` — deliberadamente paralelas a `IRefreshTokenHasher`/`IRefreshTokenGenerator` em vez de reaproveitadas: são ~15 linhas cada, e duplicar é mais barato e mais seguro do que generalizar uma abstração compartilhada tocando o código de refresh token que já está em produção conceitual (testado, sensível a segurança).
- **Nova migration**: tabela nova (`email_verification_tokens`), sem alterar `users` (o campo que ela preenche, `EmailVerifiedAt`, já existe).

### 3.3 Emissão de sessão compartilhada entre Login e Registro

`LoginUseCase` já tem ~15 linhas de "gerar access token, gerar e hashear refresh token, persistir, montar `AuthOutput`" ([LoginUseCase.cs:80-120](../../../Modules/Auth/Application/UseCases/LoginUseCase.cs)). `RegisterUseCase` precisa do mesmo resultado depois de criar a conta. Em vez de duplicar esse bloco:

- Extrair para `Modules/Auth/Application/Services/SessionIssuer.cs` (ou nome equivalente), com um método tipo `IssueAsync(User user, IReadOnlyCollection<string> roleNames, IReadOnlyCollection<string> permissions, CancellationToken) -> Task<AuthOutput>`.
- `LoginUseCase` passa a chamar esse serviço depois de validar a senha; `RegisterUseCase` chama depois de criar o usuário. Nenhum dos dois muda de comportamento observável — é remoção de duplicação, não mudança de regra.

### 3.4 `RegisterUseCase`

Dependências: `IUserRepository`, `IPasswordHasher`, `IPasswordPolicy`, `ICaptchaVerificationService`, `IEmailVerificationTokenRepository`, `IEmailVerificationTokenHasher`, `IEmailVerificationTokenGenerator`, `IEmailSender`, `SessionIssuer`, `IUnitOfWork`, `IAuditLogService`.

Fluxo (mapeado 1:1 para §6.1 da spec):

1. Verifica CAPTCHA — se inválido, `ApplicationValidationException` (`400`), nada mais roda.
2. Valida nome/e-mail/senha com os mesmos limites de `CreateUserUseCase` ([UserValidationLimits](../../../Modules/Users/Application/Validation/UserValidationLimits.cs), reaproveitado diretamente — não duplicado).
3. Dentro de `IUnitOfWork.ExecuteAsync`: checa e-mail duplicado (`ConflictException`), cria o usuário via `User.Create` (sem role nenhuma), gera e persiste o token de confirmação, grava audit log `UserRegistered` (novo nome, distinto de `UserCreated` — adicionar em [AuditLogActionNames.cs](../../../Modules/AuditLogs/Application/Constants/AuditLogActionNames.cs)).
4. **Fora** da unit of work (depois que a transação comitou): envia o e-mail de confirmação via `IEmailSender`. Enviar e-mail é uma chamada de rede; não deve rodar dentro da transação de banco nem executar se a criação falhar.
5. Emite a sessão via `SessionIssuer` e retorna.

### 3.5 `ConfirmEmailUseCase`

Dependências: `IUserRepository`, `IEmailVerificationTokenRepository`, `IEmailVerificationTokenHasher`, `IUnitOfWork`, `IAuditLogService`.

1. Hasheia o token recebido, busca por hash.
2. `NotFoundException`/`ApplicationValidationException` (`400`, ver §7 da spec) se não existir, já expirou ou já foi consumido.
3. Dentro de `IUnitOfWork`: marca o token como consumido (`TryConsumeAsync`, atômico — evita corrida de uso duplo), chama `user.MarkEmailAsVerified()` (método de domínio **que já existe e nunca foi chamado** — [User.cs:79](../../../Modules/Users/Domain/Entities/User.cs)) e persiste.

### 3.6 `ResendEmailConfirmationUseCase`

Autenticado (usa o usuário do token JWT via `ICurrentUserService`, não recebe e-mail no corpo — evita enumeração de e-mail e é consistente com "registro já autentica no mesmo request").

1. Se o e-mail já está confirmado, `ConflictException` (`409`, spec §7).
2. Dentro de `IUnitOfWork`: invalida qualquer token ativo anterior do usuário (regra 11 da spec), gera e persiste um novo.
3. Fora da unit of work: envia o novo e-mail.

### 3.7 Controller, requests, rate limiting

`Modules/Auth/Presentation/Controllers/AuthController.cs`:

- `POST /api/auth/register` — `[AllowAnonymous]`, `[EnableRateLimiting(RateLimitPolicyNames.AuthRegister)]`. Corpo: `RegisterRequest { Name, Email, Password, CaptchaToken }`. Sucesso: mesmo padrão de `LoginAsync` — seta cookie de refresh token, devolve `AuthResponse`.
- `POST /api/auth/confirm-email` — `[Authorize]` (o usuário já está autenticado desde o registro; o token de confirmação decide qual conta confirmar por carregar o `UserId`, não precisa reidentificar via JWT). Corpo: `{ Token }`.
- `POST /api/auth/resend-confirmation` — `[Authorize]`, `[EnableRateLimiting(RateLimitPolicyNames.AuthResendConfirmation)]`, sem corpo (usa `GetCurrentUserId()`, mesmo helper privado já usado em outros controllers).

`RateLimitPolicyNames` ganha `AuthRegister` e `AuthResendConfirmation`; `RateLimitOptions` ganha `Register` e `ResendConfirmation`, ambos com os mesmos valores padrão de `Login` (decisão 4 da spec: "mantenha o padrão").

### 3.8 DI

`AuthDependencyInjection.AddAuthModule`: registrar `RegisterUseCase`, `ConfirmEmailUseCase`, `ResendEmailConfirmationUseCase`, `SessionIssuer`, `IEmailVerificationTokenRepository`→`EfEmailVerificationTokenRepository`, `IEmailVerificationTokenHasher`→`Sha256EmailVerificationTokenHasher`, `IEmailVerificationTokenGenerator`→`SecureEmailVerificationTokenGenerator`, `services.AddHttpClient<ICaptchaVerificationService, TurnstileCaptchaVerificationService>()`, `services.Configure<TurnstileOptions>(configuration.GetSection("Turnstile"))`.

## 4. Courses — modelo de preço

### 4.1 Domínio

- `Modules/Courses/Domain/Enums/CoursePricingModel.cs` — `enum CoursePricingModel { Free, Paid }` (pasta nova no módulo — não existe `Domain/Enums` em Courses hoje).
- [Course.cs](../../../Modules/Courses/Domain/Entities/Course.cs): novo campo `PricingModel`, parâmetro no construtor privado, em `Create` (com valor padrão `Paid` — curso nasce pago a menos que o dono diga o contrário, mais seguro que nascer gratuito por omissão) e em `Restore`; novo método `ChangePricingModel(CoursePricingModel pricingModel)`.

### 4.2 Persistência

- `CoursePersistenceModel.cs`: nova coluna `PricingModel` (`string`).
- `CourseConfiguration.cs`: `builder.Property(x => x.PricingModel).IsRequired().HasMaxLength(20)` — mesmo padrão de tamanho pequeno usado para `VideoPersistenceModel.Status`/`StorageProvider`.
- `CourseMapper.cs`: seguir exatamente o padrão já usado em [VideoMapper.cs](../../../Modules/Media/Infrastructure/Persistence/Mappers/VideoMapper.cs) — `.ToString()` na escrita, `Enum.TryParse<CoursePricingModel>(value, ignoreCase: true, out var result)` na leitura (com fallback/exceção se o valor salvo for desconhecido) — **não** usar `HasConversion` do EF, que não é o padrão já estabelecido no projeto para esses enums.
- **Nova migration**: `AddCoursePricingModel` (ou nome equivalente), adiciona a coluna com um valor default (`Paid`) para linhas existentes.

### 4.3 Application/Presentation

- `CreateCourseInput`/`UpdateCourseInput`, `CreateCourseRequest`/`UpdateCourseRequest`, `CourseOutput`, `CourseResponse`, `CoursePresenter`: todos ganham o campo `PricingModel` (string na borda HTTP, igual `VideoStorageProvider` já faz hoje em `CreateVideoRequest`).
- `CourseInputValidator`: validar que o valor recebido é um dos nomes válidos do enum (mesmo tipo de checagem que provavelmente já existe para `StorageProvider` em Media — replicar o padrão, não inventar um novo).
- `CreateCourseUseCase`/`UpdateCourseUseCase`: passam o valor validado para `Course.Create`/`course.ChangePricingModel`.

## 5. Access — `CourseAccessService`

Único lugar que decide acesso a curso no projeto inteiro ([CourseAccessService.cs](../../../Modules/Access/Application/Services/CourseAccessService.cs)) — as duas regras novas entram só aqui.

### 5.1 `CanUserAccessCourseAsync`

Ordem de checagem (mapeada para §6.3 da spec):

1. Igual hoje: `userId`/`courseId` válidos, usuário existe e está ativo.
2. **Nova**: usuário tem `EmailVerifiedAt` preenchido — senão, nega (mesmo formato de retorno `Denied`, motivo textual próprio para diferenciar de "sem grant" nos logs/depuração).
3. Igual hoje: curso existe e está publicado.
4. **Nova, antes da checagem de Area**: se `course.PricingModel == CoursePricingModel.Free`, libera (`Allowed`) — não entra na checagem de `courseAreaIds.Count == 0` nem em nenhuma checagem de grant.
5. Daqui em diante, igual hoje: precisa de pelo menos uma Area ativa vinculada, e grant (usuário ou role) válido para alguma delas.

### 5.2 Catálogo

Novo método, não uma alteração de `ListAvailableCoursesAsync` em si — nome sugerido `ListCatalogAsync(Guid userId, CancellationToken) -> IReadOnlyCollection<CourseCatalogEntry>`, onde `CourseCatalogEntry` é um DTO de Application (`Modules/Access/Application/DTOs/CourseCatalogEntry.cs`, algo como `{ Course Course, bool HasAccess }`) — mantém o Domain (`Course`) sem saber o que é "catálogo".

Lógica (reaproveita o cálculo que `ListAvailableCoursesAsync` já faz para `accessibleAreaIds`, mas troca a fonte de cursos e o critério de inclusão):

1. Busca o usuário; se inativo ou não encontrado, todos os cursos voltam com `HasAccess = false` (não lança erro — quem decide `401` é a autenticação do controller, não este método).
2. Calcula `accessibleAreaIds` exatamente como hoje (grants de usuário + grants de role, filtrados por Area ativa e validade temporal).
3. Busca `ICourseRepository.ListPublishedAsync()` (finalmente usado) em vez de `ListByAreaIdsAsync(accessibleAreaIds)`.
4. Para cada curso: `HasAccess = emailVerified && (course.PricingModel == Free || course.AreaIds.Intersect(accessibleAreaIds).Any())`.

Areas: um método simples auxiliar (pode viver no mesmo `CourseAccessService` ou na `ListAvailableCoursesUseCase`) lista `IAreaRepository.ListAsync()` filtrado a `Active`, mesmo padrão em memória já usado em `ListAreasUseCase` (Area CRUD).

## 6. Courses (Presentation) — catálogo com filtro

### 6.1 Contratos novos

- `Modules/Courses/Presentation/Responses/AreaSummaryResponse.cs` — `{ Id, Name, Slug, DisplayOrder }` (metadados mínimos, não o `AreaResponse` administrativo completo — cliente comum não precisa de `Active`/timestamps).
- `Modules/Courses/Presentation/Responses/CourseCatalogItemResponse.cs` — campos de `CourseListItemResponse` (`Id`, `Title`, `Slug`, `Description`, `ThumbnailUrl`, `DisplayOrder`) mais `PricingModel`, `AreaIds`, `HasAccess`.
- `Modules/Courses/Presentation/Responses/CourseCatalogResponse.cs` — `{ IReadOnlyList<AreaSummaryResponse> Areas, IReadOnlyList<CourseCatalogItemResponse> Courses }`.
- `Modules/Courses/Presentation/Requests/ListAvailableCoursesRequest.cs` — `[FromQuery]`, um campo de filtro opcional (ex.: `Access` como string `all`/`granted`/`locked`, default `all` quando ausente — decisão 1 da spec: filtro via URL, não endpoint separado).

`CourseListItemResponse`/`CourseListItemOutput` antigos: avaliar se ainda são usados por outro fluxo antes de remover (checar antes de apagar, mesmo cuidado já aplicado nas specs anteriores) — se `ListAvailableCoursesUseCase` era o único consumidor, ficam órfãos e devem ser removidos junto.

### 6.2 Use case e controller

- `ListAvailableCoursesUseCase` evolui: chama `CourseAccessService.ListCatalogAsync` + a listagem de Areas ativas, aplica o filtro do input (se houver) sobre os `Courses` antes de montar a resposta (Areas nunca são filtradas — regra 8 da spec).
- `CoursesController.ListAvailableAsync`: passa a receber `[FromQuery] ListAvailableCoursesRequest`, devolve `CourseCatalogResponse` em vez do array antigo. Continua sem policy nomeada além de `[Authorize]` — **não** exige e-mail confirmado no nível do controller (regra 9 da spec: catálogo é vitrine); o filtro `HasAccess` por curso já reflete a exigência de e-mail confirmado vinda de `CourseAccessService`.

## 7. Configuração e validação de produção

`appsettings.json`: novas seções `Turnstile: { SecretKey: "" }` e `Resend: { ApiKey: "", FromAddress: "", FromName: "" }`, mesmo estilo de `Jwt`/`Media:Playback`.

`ProductionConfigurationValidator.ValidateProductionConfiguration`: adicionar `ValidateSecret(configuration["Turnstile:SecretKey"], "Turnstile:SecretKey")` e `ValidateRequired`/`ValidateSecret` equivalentes para `Resend:ApiKey`, `Resend:FromAddress` — produção não sobe sem as duas chaves, consistente com o resto do método.

## 8. Testes

### 8.1 Fakes para as duas integrações externas (obrigatório — sem chave real disponível)

Em `Tests/CourseCore.Api.Tests/Integration/Infrastructure/CourseCoreApiFactory.cs`, dentro do mesmo bloco `ConfigureServices` que já substitui `CourseCoreDbContext` por SQLite:

- `services.RemoveAll<ICaptchaVerificationService>(); services.AddSingleton<ICaptchaVerificationService, AlwaysValidCaptchaVerificationService>();` (fake em `Tests/.../TestDoubles/`, sempre retorna sucesso — CAPTCHA real não roda em CI).
- `services.RemoveAll<IEmailSender>(); services.AddSingleton<IEmailSender, InMemoryEmailSender>();` (fake que guarda os e-mails "enviados" em memória, para os testes de confirmação lerem o token gerado sem precisar de uma caixa de entrada real).

### 8.2 Domain

- `Tests/.../Domain/Users/UserTests.cs` (se ainda não cobre `MarkEmailAsVerified` — checar antes de assumir que falta) e `Tests/.../Domain/Courses/CourseTests.cs` — cobrir `PricingModel` em `Create`/`Restore`/`ChangePricingModel`.
- Novo `Tests/.../Domain/Auth/EmailVerificationTokenTests.cs`.

### 8.3 Application

- `RegisterUseCaseTests.cs` — CAPTCHA inválido, validações reaproveitadas de `CreateUserUseCase`, e-mail duplicado, sessão emitida, e-mail de confirmação "enviado" (via `InMemoryEmailSender`), sem role atribuída.
- `ConfirmEmailUseCaseTests.cs` — token válido, expirado, já consumido, inexistente.
- `ResendEmailConfirmationUseCaseTests.cs` — token anterior invalidado, e-mail já confirmado retorna conflito.
- `CourseAccessServiceTests.cs` (já existe) — adicionar casos: e-mail não confirmado nega mesmo com grant válido; curso `Free` libera sem grant e sem Area vinculada; curso `Free` despublicado continua negado; `ListCatalogAsync` traz bloqueados com `HasAccess = false` e nunca omite Areas.

### 8.4 Integração

- Novo `Tests/.../Integration/Auth/RegisterIntegrationTests.cs`: fluxo feliz (sessão emitida, cookie setado), CAPTCHA inválido → `400`, e-mail duplicado → `409`, rate limit.
- Novo (ou extensão de `AuthIntegrationTests.cs`): confirmar e-mail libera acesso a curso `Free` que antes retornava `403`; reenvio invalida token anterior; reenvio em conta já confirmada → `409`.
- `CoursesIntegrationTests.cs`: `GET /api/courses/available` sem filtro traz Areas + Courses com `HasAccess` misto; filtro `granted`/`locked` funciona; curso `Free` aparece liberado só depois de confirmar e-mail.

## 9. Documentação

- Postman: `Register` (com nota clara de que precisa de um token de Turnstile real ou de uma chave de dev vazia configurada para bypass), `Confirm Email`, `Resend Confirmation` na pasta `01 - Auth`; atualizar `Get Course Details`/listagem para refletir o novo formato de `available`.
- `Docs/postman.md`: inventário de endpoints, nota sobre e-mail confirmado ser pré-requisito de acesso a conteúdo (não do catálogo).
- `Docs/implementation-class-diagram.md`: novas entidades/use cases (`EmailVerificationToken`, `RegisterUseCase`, `ConfirmEmailUseCase`, `CoursePricingModel`).
- `README.md`: seção de autenticação ganha nota sobre registro público, CAPTCHA e confirmação de e-mail obrigatória.

## 10. Validação obrigatória

```powershell
dotnet build
dotnet test
```

```powershell
dotnet ef migrations add AddCoursePricingModel --context CourseCoreDbContext --output-dir Shared/Infrastructure/Persistence/Migrations
dotnet ef migrations add AddEmailVerificationTokens --context CourseCoreDbContext --output-dir Shared/Infrastructure/Persistence/Migrations
```

Confirmar explicitamente antes do commit:

- nenhum teste depende de chamada real a Turnstile ou Resend;
- `ProductionConfigurationValidator` bloqueia produção sem `Turnstile:SecretKey`/`Resend:ApiKey`/`Resend:FromAddress`;
- as duas migrations são revisadas manualmente (nenhuma é aplicada automaticamente, conforme `Docs/deployment-migrations.md`);
- nenhum secret real (chave de Turnstile ou Resend) é commitado;
- `CourseListItemResponse`/`CourseListItemOutput` antigos são removidos se ficarem órfãos, mantidos se outro fluxo ainda os usa.

## 11. Fora deste plano

Idêntico ao "Fora de escopo" da spec (§10): nenhuma forma de pagamento real, nenhuma recuperação de senha, nenhum convite, nenhuma mudança de multi-tenancy, nenhuma vitrine pública sem login, CAPTCHA restrito a registro (não se estende a login).
