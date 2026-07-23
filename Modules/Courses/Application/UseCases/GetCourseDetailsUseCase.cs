using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class GetCourseDetailsUseCase
{
    private readonly ICourseRepository _courses;
    private readonly CourseAccessService _courseAccessService;

    public GetCourseDetailsUseCase(
        ICourseRepository courses,
        CourseAccessService courseAccessService)
    {
        _courses = courses;
        _courseAccessService = courseAccessService;
    }

    public async Task<CourseDetailsOutput> ExecuteAsync(
        GetCourseDetailsInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(input));
        }

        if (input.CourseId == Guid.Empty)
        {
            throw new ArgumentException("CourseId is required.", nameof(input));
        }

        var course = await _courses.FindDetailsByIdAsync(input.CourseId, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException("Course not found.");
        }

        var access = await _courseAccessService.CanUserAccessCourseAsync(
            input.UserId,
            input.CourseId,
            cancellationToken);

        if (!access.CanAccess)
        {
            throw new ForbiddenException("User cannot access this course.");
        }

        return CourseDetailsOutput.FromCourse(course);
    }
}
