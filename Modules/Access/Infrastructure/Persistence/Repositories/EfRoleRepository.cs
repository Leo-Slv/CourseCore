using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Repositories;

public class EfRoleRepository : IRoleRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfRoleRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Role?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return model is null ? null : RoleMapper.ToDomain(model);
    }

    public async Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        var model = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == normalizedName, cancellationToken);

        return model is null ? null : RoleMapper.ToDomain(model);
    }

    public async Task<IReadOnlyCollection<Role>> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.Roles
            .AsNoTracking()
            .Where(x => x.UserRoles.Any(userRole => userRole.UserId == userId))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return models.Select(RoleMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyCollection<Role>> ListAsync(CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return models.Select(RoleMapper.ToDomain).ToList();
    }

    public async Task CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _dbContext.Roles.AddAsync(RoleMapper.ToPersistence(role), cancellationToken);
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Roles
            .FirstOrDefaultAsync(x => x.Id == role.Id, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Role not found.");
        }

        RoleMapper.ApplyChanges(role, model);
    }
}
