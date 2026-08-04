using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Repositories;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class ListAvailableCoursesUseCase
{
    private readonly CourseAccessService _courseAccessService;

    public ListAvailableCoursesUseCase(
        ICourseRepository courses,
        CourseAccessService courseAccessService)
    {
        _courseAccessService = courseAccessService;
    }

    public async Task<IReadOnlyCollection<CourseListItemOutput>> ExecuteAsync(
        ListAvailableCoursesInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(input));
        }

        var courses = await _courseAccessService.ListAvailableCoursesAsync(input.UserId, cancellationToken);
        return courses.OrderBy(course => course.DisplayOrder).Select(CourseListItemOutput.FromCourse).ToList();
    }
}
