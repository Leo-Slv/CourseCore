using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Repositories;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class ListRoleDetailsUseCase
{
    private readonly IRoleRepository _roles;

    public ListRoleDetailsUseCase(IRoleRepository roles)
    {
        _roles = roles;
    }

    public async Task<IReadOnlyCollection<RoleDetailOutput>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _roles.ListAsync(cancellationToken);
        var permissionsByRole = await _roles.FindPermissionKeysByRoleIdsAsync(cancellationToken);
        var memberCounts = await _roles.CountMembersByRoleIdAsync(cancellationToken);

        return roles
            .OrderBy(role => !RoleRules.IsAdmin(role))
            .ThenBy(role => role.Name)
            .Select(role => RoleDetailOutput.FromRole(
                role,
                RoleRules.IsAdmin(role),
                memberCounts.GetValueOrDefault(role.Id),
                permissionsByRole.TryGetValue(role.Id, out var keys) ? keys : []))
            .ToList();
    }
}
