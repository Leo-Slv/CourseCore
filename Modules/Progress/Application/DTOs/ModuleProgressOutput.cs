using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Progress.Domain.Entities;

namespace CourseCore.Api.Modules.Progress.Application.DTOs;

public class ModuleProgressOutput
{
    public Guid ModuleId { get; init; }

    public int LessonCount { get; init; }

    public int CompletedLessonCount { get; init; }

    public decimal ProgressPercent { get; init; }

    public static ModuleProgressOutput FromModule(
        CourseModule module,
        IReadOnlyCollection<UserLessonProgress> lessonProgresses)
    {
        var lessonIds = module.Lessons.Select(lesson => lesson.Id).ToHashSet();
        var completedCount = lessonProgresses.Count(progress => lessonIds.Contains(progress.LessonId) && progress.Completed);
        var progressPercent = lessonIds.Count == 0
            ? 0m
            : Math.Round((decimal)completedCount / lessonIds.Count * 100m, 2);

        return new ModuleProgressOutput
        {
            ModuleId = module.Id,
            LessonCount = lessonIds.Count,
            CompletedLessonCount = completedCount,
            ProgressPercent = progressPercent
        };
    }
}
