using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Courses.Domain.Repositories;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class ListAreasUseCase
{
    private readonly IAreaRepository _areas;
    private readonly ICourseRepository _courses;

    public ListAreasUseCase(IAreaRepository areas, ICourseRepository courses)
    {
        _areas = areas;
        _courses = courses;
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

        var courses = await _courses.ListAsync(cancellationToken);

        return areas
            .Select(area => AreaOutput.FromArea(
                area,
                courseCount: courses.Count(course => course.AreaIds.Contains(area.Id))))
            .ToList();
    }
}
