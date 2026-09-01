using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class GetAreaByIdUseCase
{
    private readonly IAreaRepository _areas;

    public GetAreaByIdUseCase(IAreaRepository areas)
    {
        _areas = areas;
    }

    public async Task<AreaOutput> ExecuteAsync(Guid areaId, CancellationToken cancellationToken = default)
    {
        var area = await _areas.FindByIdAsync(areaId, cancellationToken);

        if (area is null)
        {
            throw new NotFoundException("Area not found.");
        }

        return AreaOutput.FromArea(area);
    }
}
