using CourseCore.Api.Modules.Access.Domain.Entities;

namespace CourseCore.Api.Modules.Access.Domain.Repositories;

public interface IPermissionRepository
{
    Task<IReadOnlyCollection<Permission>> ListAsync(CancellationToken cancellationToken = default);
}
