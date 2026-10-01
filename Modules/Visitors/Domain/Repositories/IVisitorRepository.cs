using CourseCore.Api.Modules.Visitors.Domain.Entities;

namespace CourseCore.Api.Modules.Visitors.Domain.Repositories;

public interface IVisitorRepository
{
    Task CreateAsync(Visitor visitor, CancellationToken cancellationToken = default);

    Task<(IReadOnlyCollection<Visitor> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);
}
