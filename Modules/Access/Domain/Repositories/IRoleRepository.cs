using CourseCore.Api.Modules.Access.Domain.Entities;

namespace CourseCore.Api.Modules.Access.Domain.Repositories;

public interface IRoleRepository
{
    Task<Role?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Role>> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Role>> ListAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(Role role, CancellationToken cancellationToken = default);

    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);
}
