using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Repositories;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class ListAllCoursesUseCase
{
    private readonly ICourseRepository _courses;

    public ListAllCoursesUseCase(ICourseRepository courses)
    {
        _courses = courses;
    }

    public async Task<IReadOnlyCollection<CourseOutput>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var courses = await _courses.ListAsync(cancellationToken);

        return courses.Select(CourseOutput.FromCourse).ToList();
    }
}
