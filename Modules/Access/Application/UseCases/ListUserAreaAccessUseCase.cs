using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Repositories;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class ListUserAreaAccessUseCase
{
    private readonly IAreaRepository _areas;

    public ListUserAreaAccessUseCase(IAreaRepository areas)
    {
        _areas = areas;
    }

    public async Task<IReadOnlyCollection<AreaAccessOutput>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var accesses = await _areas.ListUserAreaAccessesAsync(userId, cancellationToken);

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
