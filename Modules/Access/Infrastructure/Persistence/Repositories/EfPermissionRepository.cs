using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Repositories;

public class EfPermissionRepository : IPermissionRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfPermissionRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Permission>> ListAsync(CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.Permissions
            .AsNoTracking()
            .OrderBy(x => x.Key)
            .ToListAsync(cancellationToken);

        return models.Select(PermissionMapper.ToDomain).ToList();
    }
}
