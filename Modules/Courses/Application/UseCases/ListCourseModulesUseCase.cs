using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class ListCourseModulesUseCase
{
    private readonly ICourseRepository _courses;
    private readonly IVideoRepository _videos;

    public ListCourseModulesUseCase(ICourseRepository courses, IVideoRepository videos)
    {
        _courses = courses;
        _videos = videos;
    }

    public async Task<IReadOnlyCollection<CourseModuleOutput>> ExecuteAsync(
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        if (courseId == Guid.Empty)
        {
            throw new ArgumentException("CourseId is required.", nameof(courseId));
        }

        var course = await _courses.FindDetailsByIdAsync(courseId, cancellationToken)
            ?? await _courses.FindByIdAsync(courseId, cancellationToken);

        if (course is null)
        {
            throw new NotFoundException("Course not found.");
        }

        var lessonIds = course.Modules
            .SelectMany(module => module.Lessons)
            .Select(lesson => lesson.Id)
            .ToList();
        var videosByLessonId = await _videos.ListByLessonIdsAsync(lessonIds, cancellationToken);
        var videoInfoByLessonId = videosByLessonId.ToDictionary(
            entry => entry.Key,
            entry => (entry.Value.Id, entry.Value.DurationSeconds));

        return course.Modules
            .OrderBy(module => module.DisplayOrder)
            .Select(module => CourseModuleOutput.FromModule(module, videoInfoByLessonId, hasAccess: true))
            .ToList();
    }
}
