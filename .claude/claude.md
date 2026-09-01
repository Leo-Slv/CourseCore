# Project Architecture

CourseCore is a modular monolith built with ASP.NET Core and .NET 10.

The codebase follows Clean Architecture and Domain-Driven Design principles,
organized primarily by business module.

Business functionality belongs under:

`Modules/<Module>/`

Existing modules include:

- Auth
- Users
- Access
- Courses
- Media
- Progress
- AuditLogs

Cross-cutting functionality belongs under:

`Shared/`

Do not create new top-level architectural structures or move responsibilities
between layers without an explicit architectural reason.

Before introducing a new pattern, inspect how equivalent functionality is
implemented in existing modules and prefer the established project convention.

## Module Structure

Modules generally follow this structure:

`Modules/<Module>/Application`
`Modules/<Module>/Domain`
`Modules/<Module>/Infrastructure`
`Modules/<Module>/Presentation`

Preserve this separation when adding functionality.

### Domain

The Domain layer contains business concepts and rules, including:

- Entities
- Value Objects
- Domain Events
- Domain Exceptions
- Policies
- Repository abstractions

Business invariants should live in the domain whenever they belong to the
domain model.

Domain code must not depend on Presentation or Infrastructure concerns.

Do not introduce persistence-specific concerns into domain entities.

### Application

The Application layer coordinates use cases.

Application code may contain:

- Use Cases
- DTOs
- application services
- contracts
- validation

Controllers should delegate business operations to application use cases
instead of implementing business workflows directly.

Use cases should depend on abstractions rather than EF Core implementations.

### Presentation

The Presentation layer contains HTTP/API concerns, including:

- Controllers
- Requests
- Responses
- Presenters

Controllers should remain thin.

Do not place business rules in controllers.

Use the existing Presenter pattern for transformations between HTTP models
and application DTOs when applicable.

Do not expose persistence models through the API.

### Infrastructure

Infrastructure contains technical implementations such as persistence,
security, storage, and external integrations.

EF Core persistence follows the existing separation between:

- Domain entities
- Persistence models
- Mappers
- EF repositories
- EF configurations

The `CourseCoreDbContext` operates on persistence models rather than domain
entities.

Repositories are responsible for translating between persistence models and
domain models using the existing mapper pattern.

Do not make domain entities EF Core persistence models unless an explicit
architectural decision changes this approach.

## Dependency Direction

Preserve the dependency direction of the existing architecture.

In general:

Presentation -> Application -> Domain

Infrastructure implements abstractions required by the inner layers.

Domain must remain independent from Infrastructure and Presentation.

Avoid introducing dependencies between modules when an existing contract,
service, or appropriate abstraction can preserve module boundaries.

## Dependency Injection

Each module owns its dependency registration through its existing
`<Module>DependencyInjection` class.

When adding module-specific services, repositories, or use cases, register
them in the corresponding module dependency injection configuration.

Shared infrastructure belongs in the existing Shared dependency registration.

Avoid registering module internals directly in `Program.cs` when they belong
to a module.

## Persistence

The project uses Entity Framework Core with PostgreSQL.

Follow the existing persistence architecture:

Domain Entity
    ↕ Mapper
Persistence Model
    ↕ EF Core
Database

When persistence schema changes are required:

- use the existing EF Core migration infrastructure;
- do not run or apply production migrations automatically from application startup;
- do not introduce secrets or environment-specific credentials into source control.

## Transactions

When a use case requires transactional behavior, follow the existing
`IUnitOfWork` pattern rather than introducing ad-hoc transaction handling.

## Cross-Cutting Concerns

Cross-cutting concerns that are shared across multiple business modules belong
under `Shared/` when appropriate.

Do not move module-specific business logic into `Shared/` merely for reuse.

Prefer keeping business concepts within their owning module.

## API Conventions

Follow the existing ASP.NET Core controller conventions and response models.

Use the project's existing exception handling infrastructure rather than
adding local try/catch blocks to controllers for normal application/domain
errors.

Respect existing authentication and authorization policies.

When an operation requires an existing permission/policy, use the established
authorization infrastructure rather than implementing permission checks
manually in controllers.

## Testing

Tests are located under:

`Tests/CourseCore.Api.Tests/`

Follow the structure and patterns of existing tests before introducing a new
testing approach.

Tests added for a feature should cover the acceptance criteria defined by its
approved specification.

Prefer testing observable behavior and business rules over implementation
details.

Do not change production architecture solely to make a test easier unless
there is a justified design reason.