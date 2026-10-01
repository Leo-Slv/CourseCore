using CourseCore.Api.Modules.Visitors.Domain.Entities;
using CourseCore.Api.Modules.Visitors.Domain.Repositories;
using CourseCore.Api.Modules.Visitors.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Visitors.Infrastructure.Persistence.Repositories;

public class EfVisitorRepository : IVisitorRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfVisitorRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(Visitor visitor, CancellationToken cancellationToken = default)
    {
        await _dbContext.Visitors.AddAsync(VisitorMapper.ToPersistence(visitor), cancellationToken);
    }

    public async Task<(IReadOnlyCollection<Visitor> Items, int TotalCount)> ListPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Visitors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var emailSearch = search.ToLowerInvariant();
            var phoneSearch = new string(search.Where(char.IsAsciiDigit).ToArray());

            query = phoneSearch.Length > 0
                ? query.Where(x => x.Name.Contains(search) || x.Email.Contains(emailSearch) || x.Phone.Contains(phoneSearch))
                : query.Where(x => x.Name.Contains(search) || x.Email.Contains(emailSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var models = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (models.Select(VisitorMapper.ToDomain).ToList(), totalCount);
    }
}
