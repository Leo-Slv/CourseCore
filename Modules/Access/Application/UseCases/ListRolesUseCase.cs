using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Repositories;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class ListRolesUseCase
{
    private readonly IRoleRepository _roles;

    public ListRolesUseCase(IRoleRepository roles)
    {
        _roles = roles;
    }

    public async Task<IReadOnlyCollection<RoleOutput>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roles.ListAsync(cancellationToken);

        return roles
            .Where(role => role.Active)
            .OrderBy(role => role.Name)
            .Select(RoleOutput.FromRole)
            .ToList();
    }
}
