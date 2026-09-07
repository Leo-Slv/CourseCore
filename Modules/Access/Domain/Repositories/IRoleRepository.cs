using CourseCore.Api.Modules.Access.Domain.Entities;

namespace CourseCore.Api.Modules.Access.Domain.Repositories;

public interface IRoleRepository
{
    Task<Role?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Role>> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> FindPermissionKeysByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Role>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> FindRoleNamesByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);

    Task CreateAsync(Role role, CancellationToken cancellationToken = default);

    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);

    Task AssignToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    Task RemoveFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}
