using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Courses.Domain.Repositories;

public interface ICourseRepository
{
    Task<Course?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Course?> FindDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Course?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Course>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Course>> ListPublishedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Course>> ListByAreaIdsAsync(
        IReadOnlyCollection<Guid> areaIds,
        CancellationToken cancellationToken = default);

    Task CreateAsync(Course course, CancellationToken cancellationToken = default);

    Task UpdateAsync(Course course, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySlugAsync(Slug slug, CancellationToken cancellationToken = default);
}
