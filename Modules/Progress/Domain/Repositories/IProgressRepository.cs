using CourseCore.Api.Modules.Progress.Domain.Entities;

namespace CourseCore.Api.Modules.Progress.Domain.Repositories;

public interface IProgressRepository
{
    Task<UserLessonProgress?> FindLessonProgressAsync(
        Guid userId,
        Guid lessonId,
        CancellationToken cancellationToken = default);

    Task<UserCourseProgress?> FindCourseProgressAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserLessonProgress>> ListLessonProgressByCourseAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default);

    Task SaveLessonProgressAsync(
        UserLessonProgress progress,
        CancellationToken cancellationToken = default);

    Task SaveCourseProgressAsync(
        UserCourseProgress progress,
        CancellationToken cancellationToken = default);
}
