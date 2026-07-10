using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Repositories;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class ListAvailableCoursesUseCase
{
    private readonly ICourseRepository _courses;
    private readonly CourseAccessService _courseAccessService;

    public ListAvailableCoursesUseCase(
        ICourseRepository courses,
        CourseAccessService courseAccessService)
    {
        _courses = courses;
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

        var publishedCourses = await _courses.ListPublishedAsync(cancellationToken);
        var availableCourses = new List<CourseListItemOutput>();

        foreach (var course in publishedCourses.OrderBy(course => course.DisplayOrder))
        {
            var access = await _courseAccessService.CanUserAccessCourseAsync(
                input.UserId,
                course.Id,
                cancellationToken);

            if (access.CanAccess)
            {
                availableCourses.Add(CourseListItemOutput.FromCourse(course));
            }
        }

        return availableCourses;
    }
}
