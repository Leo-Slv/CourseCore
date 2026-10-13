using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Repositories;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class ListPermissionsUseCase
{
    private readonly IPermissionRepository _permissions;

    public ListPermissionsUseCase(IPermissionRepository permissions)
    {
        _permissions = permissions;
    }

    public async Task<IReadOnlyCollection<PermissionOutput>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var permissions = await _permissions.ListAsync(cancellationToken);

        return permissions.Select(PermissionOutput.FromPermission).ToList();
    }
}
