using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Access.Domain.Repositories;

public interface IAreaRepository
{
    Task<Area?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Area?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Area>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Area>> ListByUserAccessAsync(Guid userId, CancellationToken cancellationToken = default);

    Task CreateAsync(Area area, CancellationToken cancellationToken = default);

    Task UpdateAsync(Area area, CancellationToken cancellationToken = default);

    Task CreateUserAreaAccessAsync(UserAreaAccess access, CancellationToken cancellationToken = default);

    Task CreateRoleAreaAccessAsync(RoleAreaAccess access, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserAreaAccess>> ListUserAreaAccessesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RoleAreaAccess>> ListRoleAreaAccessesAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default);
}
