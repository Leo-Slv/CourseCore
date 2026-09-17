using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Repositories;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class ListRoleAreaAccessUseCase
{
    private readonly IAreaRepository _areas;

    public ListRoleAreaAccessUseCase(IAreaRepository areas)
    {
        _areas = areas;
    }

    public async Task<IReadOnlyCollection<AreaAccessOutput>> ExecuteAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("RoleId is required.", nameof(roleId));
        }

        var accesses = await _areas.ListRoleAreaAccessesAsync([roleId], cancellationToken);

        return accesses
            .Select(access => new AreaAccessOutput
            {
                AreaId = access.AreaId,
                CanView = access.CanView,
                CanManage = access.CanManage
            })
            .ToList();
    }
}
