using CourseCore.Api.Modules.Courses.Domain.Entities;

namespace CourseCore.Api.Modules.Courses.Domain.Repositories;

public interface ICourseModuleRepository
{
    Task<CourseModule?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CourseModule>> ListByCourseIdAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task AddAsync(CourseModule module, CancellationToken cancellationToken = default);

    Task UpdateAsync(CourseModule module, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid moduleId, CancellationToken cancellationToken = default);

    Task ReorderAsync(Guid courseId, IReadOnlyList<Guid> orderedModuleIds, CancellationToken cancellationToken = default);
}
