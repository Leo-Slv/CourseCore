using CourseCore.Api.Modules.Courses.Domain.Entities;

namespace CourseCore.Api.Modules.Courses.Domain.Repositories;

public interface ILessonRepository
{
    Task<Lesson?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Lesson>> ListByModuleIdAsync(Guid moduleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Lesson>> ListByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task AddAsync(Lesson lesson, CancellationToken cancellationToken = default);

    Task UpdateAsync(Lesson lesson, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid lessonId, CancellationToken cancellationToken = default);

    Task ReorderAsync(Guid moduleId, IReadOnlyList<Guid> orderedLessonIds, CancellationToken cancellationToken = default);
}
