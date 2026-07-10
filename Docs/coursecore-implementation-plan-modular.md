# CourseCore API — Planejamento de Implementação Modular com Codex

## 1. Objetivo do documento

Este documento define o planejamento incremental para implementação do backend **CourseCore API** usando Codex.

A implementação deve seguir o diagrama salvo em:

```text
Docs/implementation-class-diagram.md
```

A arquitetura escolhida é:

```text
Clean Architecture / DDD modular
com separação entre entidade de domínio e entidade de persistência
```

A organização principal será por **módulos de negócio**, e não por camadas globais.

Ou seja, em vez de:

```text
Domain/
Application/
Infrastructure/
Presentation/
```

a estrutura será:

```text
Modules/
  Auth/
  Users/
  Access/
  Courses/
  Media/
  Progress/
  AuditLogs/

Shared/
```

Dentro de cada módulo, quando necessário, existirão as camadas:

```text
Application/
Domain/
Infrastructure/
Presentation/
```

Exemplo:

```text
Modules/Courses/
  Application/
  Domain/
  Infrastructure/
  Presentation/
```

A decisão arquitetural principal continua sendo:

```text
Domain Entity != Persistence Model
```

Exemplo:

```text
Modules/Courses/Domain/Entities/Course.cs
Modules/Courses/Infrastructure/Persistence/Models/CoursePersistenceModel.cs
Modules/Courses/Infrastructure/Persistence/Mappers/CourseMapper.cs
```

Este arquivo deve ser usado como referência para orientar prompts futuros. Cada etapa deve ser implementada separadamente, respeitando o escopo definido.

---

## 2. Estrutura atual esperada antes da implementação

Após resetar os commits locais de implementação, a estrutura esperada do projeto volta a estar próxima de:

```text
CourseCore/
├── Controllers/
│   └── WeatherForecastController.cs
├── Docs/
│   ├── implementation-class-diagram.md
│   └── coursecore-implementation-plan.md
├── Properties/
│   └── launchSettings.json
├── appsettings.Development.json
├── appsettings.json
├── CourseCore.csproj
├── CourseCore.http
├── CourseCore.slnx
├── Program.cs
└── WeatherForecast.cs
```

O código `WeatherForecast` é considerado placeholder do template ASP.NET e será removido somente em etapa futura.

---

## 3. Regras fixas para todos os prompts

Em todos os prompts enviados ao Codex, as seguintes regras devem ser respeitadas:

```text
1. Ler Docs/implementation-class-diagram.md antes de alterar código.
2. Ler Docs/coursecore-implementation-plan.md antes de alterar código.
3. Não implementar fora do escopo do prompt atual.
4. Manter arquitetura modular: Modules/<ModuleName>/<Layer>.
5. Manter separação entre Domain Entity e PersistenceModel.
6. Não mapear entidades de domínio diretamente no DbContext.
7. DbContext deve usar somente PersistenceModel.
8. Controllers não podem acessar banco diretamente.
9. Controllers não podem conter regra de negócio.
10. UseCases devem depender de interfaces, não de implementações concretas.
11. Repositórios EF devem converter PersistenceModel <-> Domain Entity via Mapper.
12. Usar Fluent API para configuração do EF Core.
13. O domínio não deve depender de ASP.NET, EF Core, PostgreSQL, DbContext ou PersistenceModels.
14. Ao final, rodar dotnet build.
15. Informar arquivos criados, arquivos alterados, warnings, erros e pendências.
16. Comitar apenas se o build passar e as alterações estiverem dentro do escopo.
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

## 5. Estrutura modular alvo

A estrutura base desejada é:

```text
CourseCore/
├── Modules/
│   ├── Auth/
│   │   ├── Application/
│   │   │   ├── DTOs/
│   │   │   └── UseCases/
│   │   ├── Domain/
│   │   │   ├── Entities/
│   │   │   ├── Exceptions/
│   │   │   └── Repositories/
│   │   ├── Infrastructure/
│   │   │   ├── Persistence/
│   │   │   │   ├── Models/
│   │   │   │   ├── Mappers/
│   │   │   │   ├── Configurations/
│   │   │   │   └── Repositories/
│   │   │   └── Security/
│   │   └── Presentation/
│   │       ├── Controllers/
│   │       ├── Requests/
│   │       ├── Responses/
│   │       └── Presenters/
│   │
│   ├── Users/
│   │   ├── Application/
│   │   ├── Domain/
│   │   ├── Infrastructure/
│   │   └── Presentation/
│   │
│   ├── Access/
│   │   ├── Application/
│   │   ├── Domain/
│   │   ├── Infrastructure/
│   │   └── Presentation/
│   │
│   ├── Courses/
│   │   ├── Application/
│   │   ├── Domain/
│   │   ├── Infrastructure/
│   │   └── Presentation/
│   │
│   ├── Media/
│   │   ├── Application/
│   │   ├── Domain/
│   │   ├── Infrastructure/
│   │   └── Presentation/
│   │
│   ├── Progress/
│   │   ├── Application/
│   │   ├── Domain/
│   │   ├── Infrastructure/
│   │   └── Presentation/
│   │
│   └── AuditLogs/
│       ├── Application/
│       ├── Domain/
│       ├── Infrastructure/
│       └── Presentation/
│
├── Shared/
│   ├── Application/
│   │   └── Contracts/
│   ├── Domain/
│   │   ├── Entities/
│   │   ├── Exceptions/
│   │   ├── ValueObjects/
│   │   └── Events/
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   ├── Security/
│   │   └── Storage/
│   └── Presentation/
│       ├── Filters/
│       ├── Middleware/
│       └── Responses/
│
└── Docs/
```

---

## 6. Módulos e responsabilidades

### 6.1 Auth

Responsável por autenticação e sessão.

Exemplos:

```text
Login
Refresh token
Logout
Geração de JWT
Hash de senha
Validação de credenciais
```

Caminhos principais:

```text
Modules/Auth/Application/UseCases/
Modules/Auth/Application/DTOs/
Modules/Auth/Infrastructure/Security/
Modules/Auth/Presentation/Controllers/
Modules/Auth/Presentation/Requests/
Modules/Auth/Presentation/Responses/
```

### 6.2 Users

Responsável pelo cadastro e manutenção de usuários.

Exemplos:

```text
Criar usuário
Atualizar usuário
Listar usuários
Ativar/desativar usuário
Alterar dados básicos
```

Caminhos principais:

```text
Modules/Users/Domain/Entities/User.cs
Modules/Users/Domain/Repositories/IUserRepository.cs
Modules/Users/Infrastructure/Persistence/Models/UserPersistenceModel.cs
Modules/Users/Infrastructure/Persistence/Mappers/UserMapper.cs
Modules/Users/Infrastructure/Persistence/Repositories/EfUserRepository.cs
```

### 6.3 Access

Responsável por papéis, permissões, áreas e controle de acesso.

Exemplos:

```text
Role
Permission
Area
UserAreaAccess
RoleAreaAccess
UserRole
RolePermission
Liberação individual por usuário
Liberação por papel
```

Caminhos principais:

```text
Modules/Access/Domain/Entities/
Modules/Access/Domain/Repositories/
Modules/Access/Application/UseCases/
Modules/Access/Application/Services/
Modules/Access/Infrastructure/Persistence/
Modules/Access/Presentation/
```

### 6.4 Courses

Responsável pela estrutura didática dos cursos.

Exemplos:

```text
Course
CourseModule
Lesson
CourseArea
Criação de curso
Publicação de curso
Listagem de cursos disponíveis
Detalhes do curso
```

Caminhos principais:

```text
Modules/Courses/Domain/Entities/
Modules/Courses/Domain/Repositories/
Modules/Courses/Application/UseCases/
Modules/Courses/Infrastructure/Persistence/
Modules/Courses/Presentation/
```

### 6.5 Media

Responsável por vídeos e arquivos associados.

Exemplos:

```text
Video
VideoStorageProvider
VideoStatus
Cadastro de vídeo
Solicitação de playback
Geração de URL de reprodução
Integração futura com S3, R2, Azure Blob, Vimeo ou Mux
```

Caminhos principais:

```text
Modules/Media/Domain/Entities/Video.cs
Modules/Media/Domain/Enums/
Modules/Media/Domain/Repositories/IVideoRepository.cs
Modules/Media/Application/UseCases/
Modules/Media/Application/Contracts/
Modules/Media/Infrastructure/Persistence/
Modules/Media/Infrastructure/Storage/
Modules/Media/Presentation/
```

### 6.6 Progress

Responsável pelo progresso dos usuários nos cursos e aulas.

Exemplos:

```text
UserLessonProgress
UserCourseProgress
Registrar segundos assistidos
Marcar aula como concluída
Recalcular progresso do curso
```

Caminhos principais:

```text
Modules/Progress/Domain/Entities/
Modules/Progress/Domain/Repositories/
Modules/Progress/Application/UseCases/
Modules/Progress/Infrastructure/Persistence/
Modules/Progress/Presentation/
```

### 6.7 AuditLogs

Responsável por auditoria e rastreabilidade.

Exemplos:

```text
AuditLog
Registro de criação/alteração/publicação
Consulta de logs administrativos
```

Caminhos principais:

```text
Modules/AuditLogs/Domain/Entities/AuditLog.cs
Modules/AuditLogs/Domain/Repositories/IAuditLogRepository.cs
Modules/AuditLogs/Infrastructure/Persistence/
Modules/AuditLogs/Application/UseCases/
```

### 6.8 Shared

Responsável apenas por blocos realmente transversais.

Exemplos:

```text
DomainException
EntityBase
Email
Slug
IUnitOfWork
CurrentUser
PagedResult
ApiResponse
Exception middleware
```

Não colocar em `Shared` algo que pertence claramente a um módulo específico.

---

# 7. Planejamento por posição

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
- Ler Docs/coursecore-implementation-plan.md.
- Identificar versão do .NET.
- Identificar pacotes já instalados.
- Verificar se ainda existem arquivos do template WeatherForecast.
- Propor a estrutura final modular de pastas.
```

### Não deve implementar

```text
- Não criar entidades.
- Não instalar pacotes.
- Não alterar Program.cs.
- Não criar DbContext.
- Não criar pastas.
```

### Entrega esperada

Um relatório curto dizendo:

```text
- estrutura atual encontrada;
- pacotes atuais;
- estrutura modular proposta;
- riscos encontrados;
- próximos passos.
```

---

## Posição 01 — Criar estrutura base modular de pastas e namespaces

### Objetivo

Criar a organização física do projeto conforme DDD modular.

### Escopo

Criar:

```text
Modules/
  Auth/
  Users/
  Access/
  Courses/
  Media/
  Progress/
  AuditLogs/

Shared/
```

Dentro de cada módulo, criar quando aplicável:

```text
Application/
  DTOs/
  UseCases/
  Services/
  Contracts/

Domain/
  Entities/
  ValueObjects/
  Enums/
  Exceptions/
  Repositories/
  Policies/
  Events/

Infrastructure/
  Persistence/
    Models/
    Mappers/
    Configurations/
    Repositories/

Presentation/
  Controllers/
  Requests/
  Responses/
  Presenters/
```

Para `Shared`, criar:

```text
Shared/
  Application/
    Contracts/
    DTOs/
  Domain/
    Entities/
    Exceptions/
    ValueObjects/
    Events/
  Infrastructure/
    Persistence/
    Security/
    Storage/
  Presentation/
    Filters/
    Middleware/
    Responses/
```

### Namespace base esperado

```text
CourseCore.Api
```

Exemplos:

```text
CourseCore.Api.Modules.Courses.Domain.Entities
CourseCore.Api.Modules.Courses.Application.UseCases
CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models
CourseCore.Api.Modules.Courses.Presentation.Controllers

CourseCore.Api.Shared.Domain.ValueObjects
CourseCore.Api.Shared.Application.Contracts
```

### Não deve implementar

```text
- Não criar entidades completas ainda.
- Não criar DbContext ainda.
- Não criar controllers novos ainda.
- Não mover WeatherForecast ainda.
- Não instalar pacotes.
```

### Entrega esperada

```text
- estrutura modular de pastas criada;
- arquivos .gitkeep criados onde necessário;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "chore: implement position 01 - modular architecture folders"
```

---

## Posição 02 — Criar blocos compartilhados do domínio e enums de mídia

### Objetivo

Criar as bases transversais do domínio e os enums específicos de mídia.

### Escopo Shared

Criar:

```text
Shared/Domain/Exceptions/DomainException.cs
Shared/Domain/Entities/EntityBase.cs
Shared/Domain/ValueObjects/Email.cs
Shared/Domain/ValueObjects/Slug.cs
```

### Escopo Media

Criar:

```text
Modules/Media/Domain/Enums/VideoStatus.cs
Modules/Media/Domain/Enums/VideoStorageProvider.cs
```

### Regras

`DomainException`:

```text
- herda de Exception;
- não carrega status code HTTP;
- não depende de ASP.NET.
```

`EntityBase`:

```text
- Guid Id;
- DateTime CreatedAt;
- DateTime UpdatedAt;
- MarkAsUpdated().
```

`Email`:

```text
- Value Object imutável;
- normaliza trim/lowercase;
- valida e-mail;
- igualdade por valor;
- lança DomainException se inválido.
```

`Slug`:

```text
- Value Object imutável;
- normaliza trim/lowercase;
- aceita letras minúsculas, números e hífen;
- regex sugerida: ^[a-z0-9]+(?:-[a-z0-9]+)*$;
- igualdade por valor;
- lança DomainException se inválido.
```

`VideoStatus`:

```text
Processing
Ready
Failed
```

`VideoStorageProvider`:

```text
Local
S3
AzureBlob
CloudflareR2
Vimeo
Mux
```

### Não deve implementar

```text
- Não criar User.
- Não criar Course.
- Não criar Area.
- Não criar Video.
- Não criar PersistenceModels.
- Não criar DbContext.
```

### Entrega esperada

```text
- blocos compartilhados criados;
- enums de mídia criados;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 02 - shared domain building blocks"
```

---

## Posição 03 — Criar entidades de domínio por módulo

### Objetivo

Criar as entidades reais de negócio, sem EF Core e dentro de seus módulos.

### Escopo por módulo

#### Users

```text
Modules/Users/Domain/Entities/User.cs
```

#### Access

```text
Modules/Access/Domain/Entities/Role.cs
Modules/Access/Domain/Entities/Permission.cs
Modules/Access/Domain/Entities/Area.cs
Modules/Access/Domain/Entities/UserAreaAccess.cs
Modules/Access/Domain/Entities/RoleAreaAccess.cs
```

#### Courses

```text
Modules/Courses/Domain/Entities/Course.cs
Modules/Courses/Domain/Entities/CourseModule.cs
Modules/Courses/Domain/Entities/Lesson.cs
```

#### Media

```text
Modules/Media/Domain/Entities/Video.cs
```

#### Progress

```text
Modules/Progress/Domain/Entities/UserCourseProgress.cs
Modules/Progress/Domain/Entities/UserLessonProgress.cs
```

#### AuditLogs

```text
Modules/AuditLogs/Domain/Entities/AuditLog.cs
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
User.Activate()
User.Deactivate()
User.ChangeName()
User.ChangeEmail()

Area.Activate()
Area.Deactivate()
Area.ChangeDisplayOrder()

Course.Publish()
Course.Unpublish()
Course.AddModule()
Course.RemoveModule()
Course.AttachArea()
Course.DetachArea()

CourseModule.AddLesson()
CourseModule.Publish()
CourseModule.Unpublish()

Lesson.MarkAsFreePreview()
Lesson.RemoveFreePreview()
Lesson.Publish()
Lesson.Unpublish()

Video.MarkAsProcessing()
Video.MarkAsReady()
Video.MarkAsFailed()

UserAreaAccess.IsValidAt()
UserAreaAccess.Revoke()

UserLessonProgress.RegisterWatch()
UserLessonProgress.MarkAsCompleted()

UserCourseProgress.Recalculate()
UserCourseProgress.MarkAsCompleted()
```

### Entrega esperada

```text
- entidades de domínio criadas por módulo;
- regras básicas encapsuladas;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 03 - modular domain entities"
```

---

## Posição 04 — Criar contratos de repositório e contratos compartilhados

### Objetivo

Definir interfaces que os use cases vão usar.

### Escopo Shared

Criar:

```text
Shared/Application/Contracts/IUnitOfWork.cs
Shared/Application/Contracts/ICurrentUserService.cs
```

### Escopo por módulo

#### Users

```text
Modules/Users/Domain/Repositories/IUserRepository.cs
```

#### Access

```text
Modules/Access/Domain/Repositories/IRoleRepository.cs
Modules/Access/Domain/Repositories/IAreaRepository.cs
```

#### Courses

```text
Modules/Courses/Domain/Repositories/ICourseRepository.cs
Modules/Courses/Domain/Repositories/ILessonRepository.cs
```

#### Media

```text
Modules/Media/Domain/Repositories/IVideoRepository.cs
Modules/Media/Application/Contracts/IVideoStorageService.cs
```

#### Progress

```text
Modules/Progress/Domain/Repositories/IProgressRepository.cs
```

#### Auth

```text
Modules/Auth/Application/Contracts/ITokenService.cs
Modules/Auth/Application/Contracts/IPasswordHasher.cs
```

#### AuditLogs

```text
Modules/AuditLogs/Domain/Repositories/IAuditLogRepository.cs
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
- Não criar PersistenceModels ainda.
```

### Entrega esperada

```text
- contratos criados nos módulos corretos;
- contratos compartilhados criados em Shared;
- domínio e aplicação desacoplados da infraestrutura;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 04 - modular repository contracts"
```

---

## Posição 05 — Criar PersistenceModels por módulo

### Objetivo

Criar as classes que serão mapeadas pelo EF Core, separadas das entidades de domínio.

### Escopo por módulo

#### Users

```text
Modules/Users/Infrastructure/Persistence/Models/UserPersistenceModel.cs
```

#### Access

```text
Modules/Access/Infrastructure/Persistence/Models/RolePersistenceModel.cs
Modules/Access/Infrastructure/Persistence/Models/PermissionPersistenceModel.cs
Modules/Access/Infrastructure/Persistence/Models/UserRolePersistenceModel.cs
Modules/Access/Infrastructure/Persistence/Models/RolePermissionPersistenceModel.cs
Modules/Access/Infrastructure/Persistence/Models/AreaPersistenceModel.cs
Modules/Access/Infrastructure/Persistence/Models/UserAreaAccessPersistenceModel.cs
Modules/Access/Infrastructure/Persistence/Models/RoleAreaAccessPersistenceModel.cs
```

#### Courses

```text
Modules/Courses/Infrastructure/Persistence/Models/CoursePersistenceModel.cs
Modules/Courses/Infrastructure/Persistence/Models/CourseAreaPersistenceModel.cs
Modules/Courses/Infrastructure/Persistence/Models/CourseModulePersistenceModel.cs
Modules/Courses/Infrastructure/Persistence/Models/LessonPersistenceModel.cs
```

#### Media

```text
Modules/Media/Infrastructure/Persistence/Models/VideoPersistenceModel.cs
```

#### Progress

```text
Modules/Progress/Infrastructure/Persistence/Models/UserCourseProgressPersistenceModel.cs
Modules/Progress/Infrastructure/Persistence/Models/UserLessonProgressPersistenceModel.cs
```

#### AuditLogs

```text
Modules/AuditLogs/Infrastructure/Persistence/Models/AuditLogPersistenceModel.cs
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
- métodos como Publish, Revoke ou MarkAsCompleted;
- validações de domínio.
```

### Atenção sobre relações entre módulos

Alguns PersistenceModels terão relações com modelos de outros módulos.

Exemplos:

```text
CourseAreaPersistenceModel relaciona CoursePersistenceModel e AreaPersistenceModel.
UserRolePersistenceModel relaciona UserPersistenceModel e RolePersistenceModel.
VideoPersistenceModel referencia LessonPersistenceModel.
UserLessonProgressPersistenceModel referencia UserPersistenceModel e LessonPersistenceModel.
```

Isso é permitido na infraestrutura/persistência. O domínio continua isolado.

### Entrega esperada

```text
- PersistenceModels criados por módulo;
- domínio ainda separado;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 05 - modular persistence models"
```

---

## Posição 06 — Instalar/configurar EF Core PostgreSQL e DbContext compartilhado

### Objetivo

Configurar infraestrutura de banco.

### Escopo

Verificar e instalar, se necessário:

```text
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.Design
Npgsql.EntityFrameworkCore.PostgreSQL
```

Criar o DbContext compartilhado em:

```text
Shared/Infrastructure/Persistence/CourseCoreDbContext.cs
```

O `DbContext` deve usar apenas PersistenceModels:

```text
DbSet<UserPersistenceModel>
DbSet<RolePersistenceModel>
DbSet<CoursePersistenceModel>
DbSet<LessonPersistenceModel>
DbSet<VideoPersistenceModel>
...
```

### Não pode fazer

```text
DbSet<Course>
DbSet<User>
DbSet<Video>
DbSet<Area>
```

### Observação arquitetural

Mesmo com módulos separados, o projeto terá um único `CourseCoreDbContext` inicialmente para simplificar transações e migrations.

As configurações serão carregadas por assembly ou adicionadas explicitamente em etapa posterior.

### Entrega esperada

```text
- EF Core configurado;
- DbContext compartilhado criado;
- PersistenceModels registrados;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 06 - ef core dbcontext"
```

---

## Posição 07 — Criar configurações Fluent API por módulo

### Objetivo

Configurar tabelas, colunas, chaves, índices e relacionamentos no EF Core.

### Escopo por módulo

#### Users

```text
Modules/Users/Infrastructure/Persistence/Configurations/UserConfiguration.cs
```

#### Access

```text
Modules/Access/Infrastructure/Persistence/Configurations/RoleConfiguration.cs
Modules/Access/Infrastructure/Persistence/Configurations/PermissionConfiguration.cs
Modules/Access/Infrastructure/Persistence/Configurations/UserRoleConfiguration.cs
Modules/Access/Infrastructure/Persistence/Configurations/RolePermissionConfiguration.cs
Modules/Access/Infrastructure/Persistence/Configurations/AreaConfiguration.cs
Modules/Access/Infrastructure/Persistence/Configurations/UserAreaAccessConfiguration.cs
Modules/Access/Infrastructure/Persistence/Configurations/RoleAreaAccessConfiguration.cs
```

#### Courses

```text
Modules/Courses/Infrastructure/Persistence/Configurations/CourseConfiguration.cs
Modules/Courses/Infrastructure/Persistence/Configurations/CourseAreaConfiguration.cs
Modules/Courses/Infrastructure/Persistence/Configurations/CourseModuleConfiguration.cs
Modules/Courses/Infrastructure/Persistence/Configurations/LessonConfiguration.cs
```

#### Media

```text
Modules/Media/Infrastructure/Persistence/Configurations/VideoConfiguration.cs
```

#### Progress

```text
Modules/Progress/Infrastructure/Persistence/Configurations/UserCourseProgressConfiguration.cs
Modules/Progress/Infrastructure/Persistence/Configurations/UserLessonProgressConfiguration.cs
```

#### AuditLogs

```text
Modules/AuditLogs/Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs
```

### Tabelas

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

### Regras

```text
- Usar Fluent API.
- Não usar DataAnnotations nos Domain Entities.
- Preferir índices únicos para email, slug e chaves de associação.
- Configurar relacionamentos entre PersistenceModels.
```

### Entrega esperada

```text
- Fluent API configurado por módulo;
- relacionamentos configurados;
- índices únicos aplicados;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 07 - fluent api configurations"
```

---

## Posição 08 — Criar mappers PersistenceModel ↔ Domain por módulo

### Objetivo

Implementar a conversão entre banco e domínio.

### Escopo por módulo

#### Users

```text
Modules/Users/Infrastructure/Persistence/Mappers/UserMapper.cs
```

#### Access

```text
Modules/Access/Infrastructure/Persistence/Mappers/RoleMapper.cs
Modules/Access/Infrastructure/Persistence/Mappers/PermissionMapper.cs
Modules/Access/Infrastructure/Persistence/Mappers/AreaMapper.cs
Modules/Access/Infrastructure/Persistence/Mappers/UserAreaAccessMapper.cs
Modules/Access/Infrastructure/Persistence/Mappers/RoleAreaAccessMapper.cs
```

#### Courses

```text
Modules/Courses/Infrastructure/Persistence/Mappers/CourseMapper.cs
Modules/Courses/Infrastructure/Persistence/Mappers/CourseModuleMapper.cs
Modules/Courses/Infrastructure/Persistence/Mappers/LessonMapper.cs
```

#### Media

```text
Modules/Media/Infrastructure/Persistence/Mappers/VideoMapper.cs
```

#### Progress

```text
Modules/Progress/Infrastructure/Persistence/Mappers/ProgressMapper.cs
```

#### AuditLogs

```text
Modules/AuditLogs/Infrastructure/Persistence/Mappers/AuditLogMapper.cs
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
- mappers criados por módulo;
- conversão entre domínio e persistência funcionando;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 08 - modular persistence mappers"
```

---

## Posição 09 — Implementar repositórios EF por módulo

### Objetivo

Implementar os repositórios concretos usando `CourseCoreDbContext` e mappers.

### Escopo por módulo

#### Users

```text
Modules/Users/Infrastructure/Persistence/Repositories/EfUserRepository.cs
```

#### Access

```text
Modules/Access/Infrastructure/Persistence/Repositories/EfRoleRepository.cs
Modules/Access/Infrastructure/Persistence/Repositories/EfAreaRepository.cs
```

#### Courses

```text
Modules/Courses/Infrastructure/Persistence/Repositories/EfCourseRepository.cs
Modules/Courses/Infrastructure/Persistence/Repositories/EfLessonRepository.cs
```

#### Media

```text
Modules/Media/Infrastructure/Persistence/Repositories/EfVideoRepository.cs
```

#### Progress

```text
Modules/Progress/Infrastructure/Persistence/Repositories/EfProgressRepository.cs
```

#### AuditLogs

```text
Modules/AuditLogs/Infrastructure/Persistence/Repositories/EfAuditLogRepository.cs
```

### Regras

Repositórios devem:

```text
- implementar interfaces do domínio;
- usar CourseCoreDbContext compartilhado;
- consultar PersistenceModel;
- converter para Domain usando Mapper;
- receber Domain e converter para PersistenceModel ao salvar;
- não retornar PersistenceModel para Application.
```

### Entrega esperada

```text
- repositories implementados por módulo;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 09 - modular ef repositories"
```

---

## Posição 10 — Implementar UnitOfWork compartilhado

### Objetivo

Centralizar transações.

### Escopo

Criar:

```text
Shared/Infrastructure/Persistence/EfUnitOfWork.cs
```

Deve implementar:

```text
Shared/Application/Contracts/IUnitOfWork.cs
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

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 10 - shared unit of work"
```

---

## Posição 11 — Configurar Dependency Injection por módulo

### Objetivo

Registrar dependências no container do ASP.NET.

### Escopo

Criar arquivos de DI por módulo:

```text
Modules/Auth/AuthDependencyInjection.cs
Modules/Users/UsersDependencyInjection.cs
Modules/Access/AccessDependencyInjection.cs
Modules/Courses/CoursesDependencyInjection.cs
Modules/Media/MediaDependencyInjection.cs
Modules/Progress/ProgressDependencyInjection.cs
Modules/AuditLogs/AuditLogsDependencyInjection.cs
Shared/SharedDependencyInjection.cs
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

Para chamar métodos como:

```csharp
builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddUsersModule();
builder.Services.AddAccessModule();
builder.Services.AddCoursesModule();
builder.Services.AddMediaModule();
builder.Services.AddProgressModule();
builder.Services.AddAuditLogsModule();
```

### Entrega esperada

```text
- DI configurada por módulo;
- aplicação subindo;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "chore: implement position 11 - modular dependency injection"
```

---

## Posição 12 — Implementar segurança base no módulo Auth

### Objetivo

Criar base para autenticação.

### Escopo

Criar/implementar:

```text
Modules/Auth/Application/DTOs/LoginInput.cs
Modules/Auth/Application/DTOs/AuthOutput.cs
Modules/Auth/Application/DTOs/AuthToken.cs
Modules/Auth/Application/UseCases/LoginUseCase.cs
Modules/Auth/Application/UseCases/RefreshTokenUseCase.cs
Modules/Auth/Application/Contracts/ITokenService.cs
Modules/Auth/Application/Contracts/IPasswordHasher.cs
Modules/Auth/Infrastructure/Security/JwtTokenService.cs
Modules/Auth/Infrastructure/Security/BCryptPasswordHasher.cs
```

### Observação

Nesta etapa, pode começar simples com JWT.

Refresh token pode ser implementado de forma inicial ou marcado como pendência para tabela própria depois, dependendo da complexidade.

### Entrega esperada

```text
- login use case criado;
- token service criado;
- password hasher criado;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 12 - auth security base"
```

---

## Posição 13 — Implementar use cases de Users e Access

### Objetivo

Permitir criação de usuários e controle de acesso por área.

### Escopo Users

```text
Modules/Users/Application/UseCases/CreateUserUseCase.cs
Modules/Users/Application/UseCases/UpdateUserUseCase.cs
Modules/Users/Application/UseCases/ListUsersUseCase.cs
```

### Escopo Access

```text
Modules/Access/Application/UseCases/GrantUserAreaAccessUseCase.cs
Modules/Access/Application/UseCases/GrantRoleAreaAccessUseCase.cs
Modules/Access/Application/UseCases/CheckCourseAccessUseCase.cs
Modules/Access/Application/Services/CourseAccessService.cs
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
- regra de acesso centralizada no módulo Access;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 13 - users and access use cases"
```

---

## Posição 14 — Implementar use cases de Courses

### Objetivo

Permitir criação, edição, publicação e listagem de cursos.

### Escopo

```text
Modules/Courses/Application/UseCases/CreateCourseUseCase.cs
Modules/Courses/Application/UseCases/UpdateCourseUseCase.cs
Modules/Courses/Application/UseCases/PublishCourseUseCase.cs
Modules/Courses/Application/UseCases/GetCourseDetailsUseCase.cs
Modules/Courses/Application/UseCases/ListAvailableCoursesUseCase.cs
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

A verificação de acesso deve usar o serviço do módulo Access.

### Entrega esperada

```text
- use cases de curso criados;
- acesso aplicado na listagem;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 14 - courses use cases"
```

---

## Posição 15 — Implementar use cases de Media/Videos

### Objetivo

Permitir cadastro de vídeos e geração de URL de reprodução.

### Escopo

```text
Modules/Media/Application/UseCases/CreateVideoUseCase.cs
Modules/Media/Application/UseCases/RequestVideoPlaybackUseCase.cs
Modules/Media/Infrastructure/Storage/VideoStorageService.cs
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

A verificação de acesso deve usar o serviço do módulo Access.

### Entrega esperada

```text
- vídeos cadastráveis;
- playback protegido por permissão;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 15 - media video use cases"
```

---

## Posição 16 — Implementar use cases de Progress

### Objetivo

Registrar progresso do usuário.

### Escopo

```text
Modules/Progress/Application/UseCases/RegisterLessonProgressUseCase.cs
Modules/Progress/Application/UseCases/GetCourseProgressUseCase.cs
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

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 16 - progress use cases"
```

---

## Posição 17 — Criar Requests, Responses e Presenters por módulo

### Objetivo

Criar contratos HTTP separados do domínio.

### Escopo por módulo

#### Auth

```text
Modules/Auth/Presentation/Requests/
Modules/Auth/Presentation/Responses/
Modules/Auth/Presentation/Presenters/
```

#### Users

```text
Modules/Users/Presentation/Requests/
Modules/Users/Presentation/Responses/
Modules/Users/Presentation/Presenters/
```

#### Access

```text
Modules/Access/Presentation/Requests/
Modules/Access/Presentation/Responses/
Modules/Access/Presentation/Presenters/
```

#### Courses

```text
Modules/Courses/Presentation/Requests/
Modules/Courses/Presentation/Responses/
Modules/Courses/Presentation/Presenters/
```

#### Media

```text
Modules/Media/Presentation/Requests/
Modules/Media/Presentation/Responses/
Modules/Media/Presentation/Presenters/
```

#### Progress

```text
Modules/Progress/Presentation/Requests/
Modules/Progress/Presentation/Responses/
Modules/Progress/Presentation/Presenters/
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
- requests criados por módulo;
- responses criados por módulo;
- presenters criados por módulo;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 17 - modular http contracts"
```

---

## Posição 18 — Criar controllers por módulo

### Objetivo

Expor endpoints HTTP.

### Escopo

Criar controllers:

```text
Modules/Auth/Presentation/Controllers/AuthController.cs
Modules/Users/Presentation/Controllers/UsersController.cs
Modules/Access/Presentation/Controllers/AreasController.cs
Modules/Courses/Presentation/Controllers/CoursesController.cs
Modules/Media/Presentation/Controllers/VideosController.cs
Modules/Progress/Presentation/Controllers/ProgressController.cs
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

### Sobre WeatherForecast

Nesta posição, remover o template se os controllers reais já estiverem funcionando:

```text
Controllers/WeatherForecastController.cs
WeatherForecast.cs
```

### Entrega esperada

```text
- endpoints criados;
- WeatherForecast removido, se aplicável;
- Swagger/OpenAPI exibindo endpoints;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 18 - modular controllers"
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

### Local recomendado

```text
Shared/Infrastructure/Persistence/Migrations/
```

ou o local padrão gerado pelo EF Core, desde que documentado.

### Entrega esperada

```text
- migration inicial criada;
- estrutura PostgreSQL validada;
- build funcionando.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 19 - initial database migration"
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
- senha admin documentada apenas em ambiente local;
- não executar seed destrutivo automaticamente em produção.
```

### Entrega esperada

```text
- seed inicial criado;
- aplicação testável localmente.
```

### Commit sugerido

```bash
git add .
git commit -m "feat: implement position 20 - initial seed data"
```

---

## Posição 21 — Testes básicos

### Objetivo

Garantir que o núcleo não quebre.

### Escopo

Criar testes para:

```text
Shared Domain
Modules/Courses Domain
Modules/Access Domain
Modules/Progress Domain
UseCases principais
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

### Commit sugerido

```bash
git add .
git commit -m "test: implement position 21 - core tests"
```

---

## Posição 22 — Revisão final de arquitetura

### Objetivo

Revisar aderência ao diagrama.

### Escopo

Codex deve comparar implementação com:

```text
Docs/implementation-class-diagram.md
Docs/coursecore-implementation-plan.md
```

Verificar:

```text
- estrutura modular existe;
- módulos existem;
- entidades de domínio existem nos módulos corretos;
- PersistenceModels existem nos módulos corretos;
- DbContext só usa PersistenceModel;
- repositories retornam domínio;
- controllers não acessam banco;
- use cases não acessam EF diretamente;
- mappers existem;
- DI modular está correta;
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

### Commit sugerido

```bash
git add .
git commit -m "docs: review final modular architecture implementation"
```

---

# 8. Ordem resumida

```text
00 - Diagnóstico inicial
01 - Estrutura modular de pastas
02 - Blocos compartilhados do domínio e enums de mídia
03 - Entidades de domínio por módulo
04 - Contratos por módulo e contratos shared
05 - PersistenceModels por módulo
06 - EF Core + DbContext compartilhado
07 - Fluent API por módulo
08 - Mappers por módulo
09 - Repositórios EF por módulo
10 - UnitOfWork compartilhado
11 - Dependency Injection por módulo
12 - Segurança base no Auth
13 - Users + Access use cases
14 - Courses use cases
15 - Media/Videos use cases
16 - Progress use cases
17 - Requests/Responses/Presenters por módulo
18 - Controllers por módulo
19 - Migrations PostgreSQL
20 - Seed inicial
21 - Testes
22 - Revisão final
```

---

# 9. Fluxo arquitetural desejado

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

Em estrutura modular:

```text
Modules/Courses/Presentation/Controllers/CoursesController.cs
  -> Modules/Courses/Application/UseCases/CreateCourseUseCase.cs
    -> Modules/Courses/Domain/Repositories/ICourseRepository.cs
      -> Modules/Courses/Infrastructure/Persistence/Repositories/EfCourseRepository.cs
        -> Modules/Courses/Infrastructure/Persistence/Models/CoursePersistenceModel.cs
        -> Modules/Courses/Infrastructure/Persistence/Mappers/CourseMapper.cs
        -> Modules/Courses/Domain/Entities/Course.cs
```

---

# 10. Sentido de dependência

O sentido de dependência deve respeitar:

```text
Module.Presentation -> Module.Application
Module.Application -> Module.Domain
Module.Infrastructure -> Module.Domain
Module.Infrastructure -> Shared.Infrastructure
Module.Application -> Shared.Application
Module.Domain -> Shared.Domain
```

O domínio não deve depender de:

```text
- ASP.NET
- Entity Framework Core
- PostgreSQL
- Controllers
- PersistenceModels
- DbContext
- Infrastructure
```

---

# 11. Checklist permanente de arquitetura

Durante a implementação, verificar continuamente:

```text
[ ] Estrutura está organizada em Modules/<ModuleName>/<Layer>.
[ ] Shared contém apenas recursos realmente transversais.
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

# 12. Estratégia de commits

Recomendação:

```text
Um commit por posição executada.
```

Só commitar se:

```text
- a posição foi concluída;
- dotnet build passou;
- git diff está dentro do escopo;
- não houve alteração fora da posição.
```

Se o build falhar:

```text
Não commitar.
Retornar o erro.
```

Se houver alteração fora do escopo:

```text
Não commitar.
Reportar a divergência.
```

---

# 13. Observação final

Este planejamento deve ser tratado como guia incremental de implementação.

Não tentar implementar tudo em um único prompt.

Cada posição deve ser executada, validada e revisada antes de avançar para a próxima.
