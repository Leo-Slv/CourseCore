using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class GetAreaByIdUseCase
{
    private readonly IAreaRepository _areas;
    private readonly ICourseRepository _courses;

    public GetAreaByIdUseCase(IAreaRepository areas, ICourseRepository courses)
    {
        _areas = areas;
        _courses = courses;
    }

    public async Task<AreaOutput> ExecuteAsync(Guid areaId, CancellationToken cancellationToken = default)
    {
        var area = await _areas.FindByIdAsync(areaId, cancellationToken);

        if (area is null)
        {
            throw new NotFoundException("Area not found.");
        }

        var courses = await _courses.ListAsync(cancellationToken);
        var areaCourses = courses
            .Where(course => course.AreaIds.Contains(areaId))
            .Select(course => new AreaCourseSummaryOutput
            {
                Id = course.Id,
                Title = course.Title,
                Slug = course.Slug.Value,
                Published = course.Published
            })
            .ToList();

        return AreaOutput.FromArea(area, courseCount: areaCourses.Count, courses: areaCourses);
    }
}
