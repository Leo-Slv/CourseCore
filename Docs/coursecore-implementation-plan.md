# CourseCore API — Planejamento de Implementação com Codex

## 1. Objetivo do documento

Este documento define o planejamento incremental para implementação do backend **CourseCore API** usando Codex.

A implementação deve seguir o diagrama salvo em:

```text
Docs/implementation-class-diagram.md
```

A arquitetura escolhida é baseada em:

```text
Clean Architecture / DDD modular
com separação entre entidade de domínio e entidade de persistência
```

Ou seja:

```text
Domain Entity             Persistence Model
Course                    CoursePersistenceModel
User                      UserPersistenceModel
Area                      AreaPersistenceModel
Video                     VideoPersistenceModel
```

Este arquivo deve ser usado como referência para orientar prompts futuros. Cada etapa deve ser implementada separadamente, respeitando o escopo definido.

---

## 2. Estrutura atual do projeto

Estrutura inicial informada:

```text
CourseCore/
├── bin/
│   └── Debug/
│       └── net10.0/
├── Controllers/
├── Docs/
│   └── implementation-class-diagram.md
├── obj/
│   └── Debug/
│       └── net10.0/
│           ├── ref/
│           ├── refint/
│           └── staticwebassets/
└── Properties/
```

---

## 3. Regras fixas para todos os prompts

Em todos os prompts enviados ao Codex, as seguintes regras devem ser respeitadas:

```text
1. Ler Docs/implementation-class-diagram.md antes de alterar código.
2. Não implementar fora do escopo do prompt atual.
3. Manter separação entre Domain e PersistenceModel.
4. Não fazer controller acessar banco diretamente.
5. Não colocar regra de negócio em controller.
6. Não mapear entidades de domínio diretamente no DbContext.
7. DbContext deve usar somente PersistenceModel.
8. Repositórios EF devem converter PersistenceModel <-> Domain Entity via Mapper.
9. Usar Fluent API para configuração do EF Core.
10. Ao final, rodar dotnet build.
11. Informar arquivos criados, arquivos alterados e pendências.
```

---

## 4. Como usar este planejamento

Quando uma etapa for iniciada, informe a posição correspondente.

Exemplo:

```text
Estamos na posição 00.
Gere o prompt para o Codex executar essa etapa.
```

Após o Codex executar, informe o resultado e avance para a próxima posição.

Exemplo:

```text
Resultado da posição 00:
...

Agora vamos para a posição 01.
```

A cada nova etapa, o prompt deve ser ajustado considerando o que já foi implementado e eventuais divergências encontradas.

---

# 5. Planejamento por posição

---

## Posição 00 — Diagnóstico inicial do projeto

### Objetivo

Fazer o Codex entender o projeto atual antes de criar arquivos.

### Escopo

O Codex deve:

```text
- Inspecionar a estrutura atual.
- Ler o .csproj.
- Ler Program.cs, caso exista.
- Ler Docs/implementation-class-diagram.md.
- Identificar versão do .NET.
- Identificar pacotes já instalados.
- Propor a estrutura final de pastas.
```

### Não deve implementar

```text
- Não criar entidades.
- Não instalar pacotes.
- Não alterar Program.cs.
- Não criar DbContext.
```

### Entrega esperada

Um relatório curto dizendo:

```text
- estrutura atual encontrada;
- pacotes atuais;
- estrutura proposta;
- riscos encontrados;
- próximos passos.
```

---

## Posição 01 — Criar estrutura base de pastas e namespaces

### Objetivo

Criar a organização física do projeto conforme a arquitetura.

### Escopo

Criar pastas como:

```text
Domain/
  Entities/
  ValueObjects/
  Enums/
  Exceptions/
  Repositories/

Application/
  Contracts/
  DTOs/
  Services/
  UseCases/

Infrastructure/
  Persistence/
    Models/
    Configurations/
    Mappers/
    Repositories/
  Security/
  Storage/

Presentation/
  Requests/
  Responses/
  Presenters/

Shared/
```

### Não deve implementar

```text
- Não criar entidades completas ainda.
- Não criar DbContext ainda.
- Não criar controllers novos ainda.
```

### Entrega esperada

```text
- estrutura de pastas criada;
- namespaces coerentes;
- build ainda funcionando.
```

---

## Posição 02 — Criar blocos compartilhados do domínio

### Objetivo

Criar as bases que serão usadas pelas entidades.

### Escopo

Implementar:

```text
DomainException
BaseEntity ou EntityBase
Email
Slug
VideoStatus
VideoStorageProvider
```

Possíveis arquivos:

```text
Domain/Exceptions/DomainException.cs
Domain/Entities/EntityBase.cs
Domain/ValueObjects/Email.cs
Domain/ValueObjects/Slug.cs
Domain/Enums/VideoStatus.cs
Domain/Enums/VideoStorageProvider.cs
```

### Não deve implementar

```text
- Não criar Course ainda.
- Não criar User ainda.
- Não criar banco.
```

### Entrega esperada

```text
- value objects validando dados básicos;
- enums criados;
- exception base criada;
- build funcionando.
```

---

## Posição 03 — Criar entidades de domínio principais

### Objetivo

Criar as entidades reais de negócio, sem EF Core.

### Escopo

Implementar entidades em:

```text
Domain/Entities/
```

Entidades:

```text
User
Role
Permission
Area
Course
CourseModule
Lesson
Video
UserAreaAccess
RoleAreaAccess
UserCourseProgress
UserLessonProgress
AuditLog
```

### Regras

As entidades devem ter:

```text
- propriedades com private set quando possível;
- métodos de comportamento;
- validações de domínio;
- nada de atributos [Table], [Column], [Key];
- nada de referência ao EF Core;
- nada de DbContext;
- nada de DataAnnotations de banco.
```

### Exemplos de métodos esperados

```text
Course.Publish()
Course.Unpublish()
Course.AddModule()
Course.AttachArea()

Lesson.MarkAsFreePreview()
Lesson.Publish()

Video.MarkAsProcessing()
Video.MarkAsReady()
Video.MarkAsFailed()

UserAreaAccess.IsValidAt()
UserAreaAccess.Revoke()

UserLessonProgress.RegisterWatch()
UserLessonProgress.MarkAsCompleted()
```

### Entrega esperada

```text
- entidades de domínio criadas;
- regras básicas encapsuladas;
- build funcionando.
```

---

## Posição 04 — Criar contratos de repositório e contratos de aplicação

### Objetivo

Definir interfaces que os use cases vão usar.

### Escopo

Criar em:

```text
Domain/Repositories/
Application/Contracts/
```

Interfaces:

```text
IUserRepository
IRoleRepository
IAreaRepository
ICourseRepository
ILessonRepository
IVideoRepository
IProgressRepository
IUnitOfWork
ITokenService
IPasswordHasher
IVideoStorageService
ICurrentUserService
```

### Regra importante

As interfaces de repositório devem retornar entidades de domínio, não `PersistenceModel`.

Exemplo:

```csharp
Task<Course?> FindByIdAsync(Guid id);
Task CreateAsync(Course course);
Task UpdateAsync(Course course);
```

### Não deve implementar

```text
- Não criar EF repositories ainda.
- Não criar DbContext ainda.
```

### Entrega esperada

```text
- contratos criados;
- domínio e aplicação desacoplados da infraestrutura;
- build funcionando.
```

---

## Posição 05 — Criar PersistenceModels do EF Core

### Objetivo

Criar as classes que serão mapeadas pelo EF Core.

### Escopo

Criar em:

```text
Infrastructure/Persistence/Models/
```

Classes:

```text
UserPersistenceModel
RolePersistenceModel
PermissionPersistenceModel
UserRolePersistenceModel
RolePermissionPersistenceModel
AreaPersistenceModel
CoursePersistenceModel
CourseAreaPersistenceModel
CourseModulePersistenceModel
LessonPersistenceModel
VideoPersistenceModel
UserAreaAccessPersistenceModel
RoleAreaAccessPersistenceModel
UserCourseProgressPersistenceModel
UserLessonProgressPersistenceModel
AuditLogPersistenceModel
```

### Regra principal

Essas classes são modelos de banco.

Podem ter:

```text
- get; set;
- ICollection;
- navigation properties;
- campos simples string, Guid, DateTime, bool, decimal.
```

Não devem ter:

```text
- regra de negócio;
- métodos como Publish ou Revoke;
- validações de domínio.
```

### Entrega esperada

```text
- PersistenceModels criados;
- domínio ainda separado;
- build funcionando.
```

---

## Posição 06 — Instalar/configurar EF Core PostgreSQL e DbContext

### Objetivo

Configurar infraestrutura de banco.

### Escopo

Verificar e instalar, se necessário:

```text
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.Design
Npgsql.EntityFrameworkCore.PostgreSQL
```

Criar:

```text
Infrastructure/Persistence/CourseCoreDbContext.cs
```

O `DbContext` deve usar apenas:

```text
DbSet<UserPersistenceModel>
DbSet<CoursePersistenceModel>
DbSet<LessonPersistenceModel>
...
```

### Não pode fazer

```text
DbSet<Course>
DbSet<User>
DbSet<Video>
```

### Entrega esperada

```text
- EF Core configurado;
- DbContext criado;
- PersistenceModels registrados;
- build funcionando.
```

---

## Posição 07 — Criar configurações Fluent API

### Objetivo

Configurar tabelas, colunas, chaves e relacionamentos no EF Core.

### Escopo

Criar em:

```text
Infrastructure/Persistence/Configurations/
```

Configurações:

```text
UserConfiguration
RoleConfiguration
PermissionConfiguration
UserRoleConfiguration
RolePermissionConfiguration
AreaConfiguration
CourseConfiguration
CourseAreaConfiguration
CourseModuleConfiguration
LessonConfiguration
VideoConfiguration
UserAreaAccessConfiguration
RoleAreaAccessConfiguration
UserCourseProgressConfiguration
UserLessonProgressConfiguration
AuditLogConfiguration
```

### Regras

Usar tabelas em padrão PostgreSQL:

```text
users
roles
permissions
user_roles
role_permissions
areas
courses
course_areas
course_modules
lessons
videos
user_area_accesses
role_area_accesses
user_course_progress
user_lesson_progress
audit_logs
```

### Entrega esperada

```text
- Fluent API configurado;
- relacionamentos configurados;
- índices únicos aplicados;
- build funcionando.
```

---

## Posição 08 — Criar mappers PersistenceModel ↔ Domain

### Objetivo

Implementar a conversão entre banco e domínio.

### Escopo

Criar em:

```text
Infrastructure/Persistence/Mappers/
```

Mappers:

```text
UserMapper
RoleMapper
PermissionMapper
AreaMapper
CourseMapper
CourseModuleMapper
LessonMapper
VideoMapper
UserAreaAccessMapper
RoleAreaAccessMapper
ProgressMapper
AuditLogMapper
```

### Regras

Cada mapper deve ter métodos como:

```csharp
ToDomain(PersistenceModel model)
ToPersistence(DomainEntity entity)
ApplyChanges(DomainEntity entity, PersistenceModel model)
```

### Observação importante

Em `Update`, preferir:

```text
buscar PersistenceModel existente no banco
aplicar alterações via ApplyChanges
salvar
```

Em vez de sempre criar um novo objeto.

### Entrega esperada

```text
- mappers criados;
- conversão entre domínio e persistência funcionando;
- build funcionando.
```

---

## Posição 09 — Implementar repositórios EF

### Objetivo

Implementar os repositórios concretos usando `DbContext` e mappers.

### Escopo

Criar em:

```text
Infrastructure/Persistence/Repositories/
```

Classes:

```text
EfUserRepository
EfRoleRepository
EfAreaRepository
EfCourseRepository
EfLessonRepository
EfVideoRepository
EfProgressRepository
```

### Regras

Repositórios devem:

```text
- implementar interfaces do domínio;
- usar CourseCoreDbContext;
- consultar PersistenceModel;
- converter para Domain usando Mapper;
- receber Domain e converter para PersistenceModel ao salvar;
- não retornar PersistenceModel para Application.
```

### Entrega esperada

```text
- repositories implementados;
- build funcionando.
```

---

## Posição 10 — Implementar UnitOfWork

### Objetivo

Centralizar transações.

### Escopo

Criar:

```text
Infrastructure/Persistence/EfUnitOfWork.cs
```

Deve implementar:

```text
IUnitOfWork
```

Com métodos:

```csharp
ExecuteAsync<T>(Func<Task<T>> action)
ExecuteAsync(Func<Task> action)
```

### Regras

O `UnitOfWork` deve:

```text
- abrir transação;
- executar ação;
- salvar alterações;
- commit em sucesso;
- rollback em erro.
```

### Entrega esperada

```text
- UnitOfWork criado;
- build funcionando.
```

---

## Posição 11 — Configurar Dependency Injection

### Objetivo

Registrar dependências no container do ASP.NET.

### Escopo

Criar:

```text
Infrastructure/DependencyInjection.cs
Application/DependencyInjection.cs
```

Registrar:

```text
DbContext
Repositories
UnitOfWork
Mappers
PasswordHasher
TokenService
StorageService
UseCases
Application Services
```

Alterar:

```text
Program.cs
```

### Entrega esperada

```text
- DI configurada;
- aplicação subindo;
- build funcionando.
```

---

## Posição 12 — Implementar segurança base

### Objetivo

Criar base para autenticação.

### Escopo

Criar:

```text
Infrastructure/Security/JwtTokenService.cs
Infrastructure/Security/BCryptPasswordHasher.cs
Application/DTOs/Auth/
Application/UseCases/Auth/
```

Implementar:

```text
LoginUseCase
RefreshTokenUseCase
JwtTokenService
BCryptPasswordHasher
```

### Observação

Nesta etapa, podemos começar simples com JWT. Refresh token pode ser deixado básico ou planejado para tabela própria depois, dependendo do escopo desejado.

### Entrega esperada

```text
- login use case criado;
- token service criado;
- password hasher criado;
- build funcionando.
```

---

## Posição 13 — Implementar use cases de Users e Access

### Objetivo

Permitir criação de usuários e controle de acesso por área.

### Escopo

Implementar:

```text
CreateUserUseCase
UpdateUserUseCase
ListUsersUseCase
GrantUserAreaAccessUseCase
GrantRoleAreaAccessUseCase
CheckCourseAccessUseCase
CourseAccessService
```

### Regras

`CourseAccessService` deve considerar:

```text
- acesso via role;
- acesso individual via UserAreaAccess;
- período de validade StartsAt / ExpiresAt;
- usuário ativo;
- área ativa.
```

### Entrega esperada

```text
- use cases criados;
- regra de acesso centralizada;
- build funcionando.
```

---

## Posição 14 — Implementar use cases de Courses

### Objetivo

Permitir criação, edição, publicação e listagem de cursos.

### Escopo

Implementar:

```text
CreateCourseUseCase
UpdateCourseUseCase
PublishCourseUseCase
GetCourseDetailsUseCase
ListAvailableCoursesUseCase
```

### Regras

Cursos devem:

```text
- vincular áreas;
- conter módulos;
- conter aulas;
- respeitar regra de publicação;
- só serem listados para usuários autorizados.
```

### Entrega esperada

```text
- use cases de curso criados;
- acesso aplicado na listagem;
- build funcionando.
```

---

## Posição 15 — Implementar use cases de Media/Videos

### Objetivo

Permitir cadastro de vídeos e geração de URL de reprodução.

### Escopo

Implementar:

```text
CreateVideoUseCase
RequestVideoPlaybackUseCase
VideoStorageService
```

### Regras

`RequestVideoPlaybackUseCase` deve:

```text
- buscar vídeo;
- identificar aula/curso relacionado;
- verificar permissão do usuário;
- gerar URL de playback;
- não devolver StorageKey sensível se não for necessário.
```

### Entrega esperada

```text
- vídeos cadastráveis;
- playback protegido por permissão;
- build funcionando.
```

---

## Posição 16 — Implementar use cases de Progress

### Objetivo

Registrar progresso do usuário.

### Escopo

Implementar:

```text
RegisterLessonProgressUseCase
GetCourseProgressUseCase
```

### Regras

Progresso deve:

```text
- registrar segundos assistidos;
- marcar aula como concluída;
- recalcular percentual do curso;
- validar usuário e aula;
- evitar reduzir progresso indevidamente.
```

### Entrega esperada

```text
- progresso de aula funcionando;
- progresso de curso funcionando;
- build funcionando.
```

---

## Posição 17 — Criar Requests, Responses e Presenters

### Objetivo

Criar contratos HTTP separados do domínio.

### Escopo

Criar:

```text
Presentation/Requests/
Presentation/Responses/
Presentation/Presenters/
```

Para:

```text
Auth
Users
Areas
Courses
Videos
Progress
```

### Regras

Requests e responses:

```text
- não devem ser entidades de domínio;
- não devem ser PersistenceModel;
- devem representar somente contrato HTTP.
```

### Entrega esperada

```text
- requests criados;
- responses criados;
- presenters criados;
- build funcionando.
```

---

## Posição 18 — Criar controllers

### Objetivo

Expor endpoints HTTP.

### Escopo

Criar controllers:

```text
AuthController
UsersController
AreasController
CoursesController
VideosController
ProgressController
```

### Regras

Controllers devem:

```text
- receber request;
- chamar use case;
- usar presenter;
- retornar response;
- não acessar DbContext;
- não acessar EF repository concreto;
- não conter regra de negócio.
```

### Entrega esperada

```text
- endpoints criados;
- Swagger exibindo endpoints;
- build funcionando.
```

---

## Posição 19 — Criar migrations e validar PostgreSQL

### Objetivo

Gerar o banco a partir dos PersistenceModels.

### Escopo

Codex deve:

```text
- verificar connection string;
- configurar appsettings;
- gerar migration inicial;
- validar se as tabelas foram geradas conforme esperado;
- não apagar dados existentes sem autorização.
```

### Entrega esperada

```text
- migration inicial criada;
- estrutura PostgreSQL validada;
- build funcionando.
```

---

## Posição 20 — Criar seed inicial

### Objetivo

Criar dados mínimos para testar.

### Escopo

Seed com:

```text
- roles: Admin, Student, Instructor
- permissions básicas
- área inicial
- usuário admin
- curso exemplo opcional
```

### Regras

Seed deve ser seguro:

```text
- não duplicar registros;
- usar upsert ou checagem por chave única;
- senha admin documentada apenas em ambiente local.
```

### Entrega esperada

```text
- seed inicial criado;
- aplicação testável localmente.
```

---

## Posição 21 — Testes básicos

### Objetivo

Garantir que o núcleo não quebre.

### Escopo

Criar testes para:

```text
Domain
UseCases
Repositories principais
```

Prioridade:

```text
Course.Publish()
UserAreaAccess.IsValidAt()
UserLessonProgress.RegisterWatch()
CourseAccessService.CanUserAccessCourseAsync()
CreateCourseUseCase
```

### Entrega esperada

```text
- testes criados;
- dotnet test funcionando.
```

---

## Posição 22 — Revisão final de arquitetura

### Objetivo

Revisar aderência ao diagrama.

### Escopo

Codex deve comparar implementação com:

```text
Docs/implementation-class-diagram.md
```

Verificar:

```text
- entidades de domínio existem;
- PersistenceModels existem;
- DbContext só usa PersistenceModel;
- repositories retornam domínio;
- controllers não acessam banco;
- use cases não acessam EF diretamente;
- mappers existem;
- DI está correta;
- build/test passam.
```

### Entrega esperada

Relatório com:

```text
- implementado;
- divergente;
- pendente;
- riscos;
- próximos ajustes.
```

---

# 6. Ordem resumida

```text
00 - Diagnóstico inicial
01 - Estrutura de pastas
02 - Blocos compartilhados do domínio
03 - Entidades de domínio
04 - Contratos de repositório e aplicação
05 - PersistenceModels
06 - EF Core + DbContext
07 - Fluent API
08 - Mappers
09 - Repositórios EF
10 - UnitOfWork
11 - Dependency Injection
12 - Segurança base
13 - Users + Access
14 - Courses
15 - Media/Videos
16 - Progress
17 - Requests/Responses/Presenters
18 - Controllers
19 - Migrations PostgreSQL
20 - Seed inicial
21 - Testes
22 - Revisão final
```

---

# 7. Fluxo arquitetural desejado

O fluxo geral da aplicação deve seguir:

```text
Controller
  -> UseCase
    -> Repository Interface
      -> EfRepository
        -> PersistenceModel via DbContext
        -> Mapper
        -> Domain Entity
```

O sentido de dependência deve respeitar:

```text
Presentation -> Application -> Domain
Infrastructure -> Domain
Infrastructure -> Application Contracts
Application -> Domain
```

O domínio não deve depender de:

```text
- ASP.NET
- Entity Framework Core
- PostgreSQL
- Controllers
- PersistenceModels
- DbContext
```

---

# 8. Checklist permanente de arquitetura

Durante a implementação, verificar continuamente:

```text
[ ] Controller não acessa DbContext.
[ ] Controller não acessa repositório EF concreto.
[ ] Controller não possui regra de negócio.
[ ] UseCase não acessa DbContext diretamente.
[ ] UseCase depende de interfaces.
[ ] Repository Interface retorna entidade de domínio.
[ ] EfRepository usa PersistenceModel internamente.
[ ] DbContext possui apenas DbSet de PersistenceModel.
[ ] Mapper converte PersistenceModel para Domain Entity.
[ ] Mapper converte Domain Entity para PersistenceModel.
[ ] Domain Entity não possui atributos de EF Core.
[ ] Domain Entity possui comportamento, não apenas propriedades.
[ ] PersistenceModel não possui regra de negócio.
[ ] Configuração de banco está em Fluent API.
[ ] dotnet build passa ao final da etapa.
```

---

# 9. Observação final

Este planejamento deve ser tratado como guia incremental de implementação.

Não tentar implementar tudo em um único prompt.

Cada posição deve ser executada, validada e revisada antes de avançar para a próxima.
