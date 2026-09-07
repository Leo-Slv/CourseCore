using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;
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
            .Where(x => x.Active && x.UserRoles.Any(userRole => userRole.UserId == userId))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return models.Select(RoleMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyCollection<string>> FindPermissionKeysByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(x => x.Role != null
                && x.Permission != null
                && x.Role.Active
                && x.Role.UserRoles.Any(userRole => userRole.UserId == userId))
            .Select(x => x.Permission!.Key)
            .Distinct()
            .OrderBy(key => key)
            .ToListAsync(cancellationToken);
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

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> FindRoleNamesByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyCollection<string>>();
        }

        var rows = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId) && x.Role != null && x.Role.Active)
            .Select(x => new { x.UserId, RoleName = x.Role!.Name })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group.Select(row => row.RoleName).ToList());
    }

    public async Task AssignToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.RoleId == roleId, cancellationToken);

        if (exists)
        {
            return;
        }

        await _dbContext.UserRoles.AddAsync(
            new UserRolePersistenceModel
            {
                UserId = userId,
                RoleId = roleId,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);
    }

    public async Task RemoveFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.UserRoles
            .FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId, cancellationToken);

        if (model is not null)
        {
            _dbContext.UserRoles.Remove(model);
        }
    }
}
