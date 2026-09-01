using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Repositories;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class ListAreasUseCase
{
    private readonly IAreaRepository _areas;

    public ListAreasUseCase(IAreaRepository areas)
    {
        _areas = areas;
    }

    public async Task<IReadOnlyCollection<AreaOutput>> ExecuteAsync(
        ListAreasInput input,
        CancellationToken cancellationToken = default)
    {
        var areas = await _areas.ListAsync(cancellationToken);

        if (input.Active is not null)
        {
            areas = areas.Where(area => area.Active == input.Active).ToList();
        }

        return areas.Select(AreaOutput.FromArea).ToList();
    }
}
