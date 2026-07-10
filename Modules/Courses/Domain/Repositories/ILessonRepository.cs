using CourseCore.Api.Modules.Courses.Domain.Entities;

namespace CourseCore.Api.Modules.Courses.Domain.Repositories;

public interface ILessonRepository
{
    Task<Lesson?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Lesson>> ListByModuleIdAsync(Guid moduleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Lesson>> ListByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);
}
