using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class GetCourseDetailsUseCase
{
    private readonly ICourseRepository _courses;
    private readonly CourseAccessService _courseAccessService;
    private readonly IVideoRepository _videos;

    public GetCourseDetailsUseCase(
        ICourseRepository courses,
        CourseAccessService courseAccessService,
        IVideoRepository videos)
    {
        _courses = courses;
        _courseAccessService = courseAccessService;
        _videos = videos;
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

        var lessonIds = course.Modules
            .SelectMany(module => module.Lessons)
            .Select(lesson => lesson.Id)
            .ToList();
        var videosByLessonId = await _videos.ListByLessonIdsAsync(lessonIds, cancellationToken);
        var videoInfoByLessonId = videosByLessonId.ToDictionary(
            entry => entry.Key,
            entry => (entry.Value.Id, entry.Value.DurationSeconds));

        return CourseDetailsOutput.FromCourse(course, videoInfoByLessonId);
    }
}
