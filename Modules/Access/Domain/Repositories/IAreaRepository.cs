using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Access.Domain.Repositories;

public interface IAreaRepository
{
    Task<Area?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Area?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Area>> ListAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(Area area, CancellationToken cancellationToken = default);

    Task UpdateAsync(Area area, CancellationToken cancellationToken = default);
}
