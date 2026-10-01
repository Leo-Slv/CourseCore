using CourseCore.Api.Modules.Visitors.Domain.Entities;
using CourseCore.Api.Modules.Visitors.Domain.Repositories;

namespace CourseCore.Api.Tests.TestDoubles;

public sealed class FakeVisitorRepository : IVisitorRepository
{
    public List<Visitor> Visitors { get; } = [];

    public string? LastSearch { get; private set; }

    public Task CreateAsync(Visitor visitor, CancellationToken cancellationToken = default)
    {
        Visitors.Add(visitor);

        return Task.CompletedTask;
    }

    public Task<(IReadOnlyCollection<Visitor> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        LastSearch = search;

        var items = Visitors
            .OrderByDescending(visitor => visitor.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return Task.FromResult<(IReadOnlyCollection<Visitor>, int)>((items, Visitors.Count));
    }
}
