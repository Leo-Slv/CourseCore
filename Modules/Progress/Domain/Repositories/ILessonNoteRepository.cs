using CourseCore.Api.Modules.Progress.Domain.Entities;

namespace CourseCore.Api.Modules.Progress.Domain.Repositories;

public interface ILessonNoteRepository
{
    Task<LessonNote?> FindByUserAndLessonAsync(
        Guid userId,
        Guid lessonId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(LessonNote note, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken = default);
}
