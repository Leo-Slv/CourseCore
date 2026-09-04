using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Progress.Domain.Entities;

namespace CourseCore.Api.Modules.Progress.Application.DTOs;

public class CourseProgressOutput
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public Guid CourseId { get; init; }

    public decimal ProgressPercent { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime? CompletedAt { get; init; }

    public IReadOnlyCollection<LessonProgressOutput> Lessons { get; init; } = Array.Empty<LessonProgressOutput>();

    public IReadOnlyCollection<ModuleProgressOutput> Modules { get; init; } = Array.Empty<ModuleProgressOutput>();

    public static CourseProgressOutput FromProgress(
        UserCourseProgress progress,
        Course course,
        IReadOnlyCollection<UserLessonProgress> lessons)
    {
        return new CourseProgressOutput
        {
            Id = progress.Id,
            UserId = progress.UserId,
            CourseId = progress.CourseId,
            ProgressPercent = progress.ProgressPercent,
            StartedAt = progress.StartedAt,
            CompletedAt = progress.CompletedAt,
            Lessons = lessons
                .OrderBy(lesson => lesson.LastWatchedAt)
                .Select(LessonProgressOutput.FromProgress)
                .ToList(),
            Modules = BuildModules(course, lessons)
        };
    }

    public static CourseProgressOutput Empty(
        Guid userId,
        Guid courseId,
        Course course,
        IReadOnlyCollection<UserLessonProgress> lessons)
    {
        return new CourseProgressOutput
        {
            Id = Guid.Empty,
            UserId = userId,
            CourseId = courseId,
            ProgressPercent = 0,
            StartedAt = DateTime.MinValue,
            CompletedAt = null,
            Lessons = lessons
                .OrderBy(lesson => lesson.LastWatchedAt)
                .Select(LessonProgressOutput.FromProgress)
                .ToList(),
            Modules = BuildModules(course, lessons)
        };
    }

    private static IReadOnlyCollection<ModuleProgressOutput> BuildModules(
        Course course,
        IReadOnlyCollection<UserLessonProgress> lessons)
    {
        return course.Modules
            .OrderBy(module => module.DisplayOrder)
            .Select(module => ModuleProgressOutput.FromModule(module, lessons))
            .ToList();
    }
}
