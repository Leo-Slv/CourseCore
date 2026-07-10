using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Progress.Application.DTOs;
using CourseCore.Api.Modules.Progress.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;

namespace CourseCore.Api.Modules.Progress.Application.UseCases;

public class GetCourseProgressUseCase
{
    private readonly IUserRepository _users;
    private readonly ICourseRepository _courses;
    private readonly IProgressRepository _progress;
    private readonly CourseAccessService _courseAccessService;

    public GetCourseProgressUseCase(
        IUserRepository users,
        ICourseRepository courses,
        IProgressRepository progress,
        CourseAccessService courseAccessService)
    {
        _users = users;
        _courses = courses;
        _progress = progress;
        _courseAccessService = courseAccessService;
    }

    public async Task<CourseProgressOutput> ExecuteAsync(
        GetCourseProgressInput input,
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

        var user = await _users.FindByIdAsync(input.UserId, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var course = await _courses.FindDetailsByIdAsync(input.CourseId, cancellationToken)
            ?? await _courses.FindByIdAsync(input.CourseId, cancellationToken);

        if (course is null)
        {
            throw new InvalidOperationException("Course not found.");
        }

        var access = await _courseAccessService.CanUserAccessCourseAsync(
            input.UserId,
            input.CourseId,
            cancellationToken);

        if (!access.CanAccess)
        {
            throw new UnauthorizedAccessException("User cannot access this course.");
        }

        var courseProgress = await _progress.FindCourseProgressAsync(
            input.UserId,
            input.CourseId,
            cancellationToken);

        var lessonProgresses = await _progress.ListLessonProgressByCourseAsync(
            input.UserId,
            input.CourseId,
            cancellationToken);

        if (courseProgress is null)
        {
            return CourseProgressOutput.Empty(input.UserId, input.CourseId, lessonProgresses);
        }

        return CourseProgressOutput.FromProgress(courseProgress, lessonProgresses);
    }
}
