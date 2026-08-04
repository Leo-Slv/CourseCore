using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Domain.ValueObjects;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Repositories;

public class EfAreaRepository : IAreaRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfAreaRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Area?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Areas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return model is null ? null : AreaMapper.ToDomain(model);
    }

    public async Task<Area?> FindBySlugAsync(Slug slug, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Areas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug.Value, cancellationToken);

        return model is null ? null : AreaMapper.ToDomain(model);
    }

    public async Task<IReadOnlyCollection<Area>> ListAsync(CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.Areas
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return models.Select(AreaMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyCollection<Area>> ListByUserAccessAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.Areas
            .AsNoTracking()
            .Where(x => x.UserAccesses.Any(access => access.UserId == userId && (access.CanView || access.CanManage)))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return models.Select(AreaMapper.ToDomain).ToList();
    }

    public async Task CreateAsync(Area area, CancellationToken cancellationToken = default)
    {
        await _dbContext.Areas.AddAsync(AreaMapper.ToPersistence(area), cancellationToken);
    }

    public async Task UpdateAsync(Area area, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.Areas
            .FirstOrDefaultAsync(x => x.Id == area.Id, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Area not found.");
        }

        AreaMapper.ApplyChanges(area, model);
    }

    public async Task CreateUserAreaAccessAsync(UserAreaAccess access, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserAreaAccesses.AddAsync(UserAreaAccessMapper.ToPersistence(access), cancellationToken);
    }

    public async Task CreateRoleAreaAccessAsync(RoleAreaAccess access, CancellationToken cancellationToken = default)
    {
        await _dbContext.RoleAreaAccesses.AddAsync(RoleAreaAccessMapper.ToPersistence(access), cancellationToken);
    }

    public async Task<UserAreaAccess?> FindUserAreaAccessAsync(Guid userId, Guid areaId, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.UserAreaAccesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.AreaId == areaId, cancellationToken);
        return model is null ? null : UserAreaAccessMapper.ToDomain(model);
    }

    public async Task<RoleAreaAccess?> FindRoleAreaAccessAsync(Guid roleId, Guid areaId, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.RoleAreaAccesses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.RoleId == roleId && x.AreaId == areaId, cancellationToken);
        return model is null ? null : RoleAreaAccessMapper.ToDomain(model);
    }

    public async Task UpdateUserAreaAccessAsync(UserAreaAccess access, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.UserAreaAccesses.FirstAsync(x => x.Id == access.Id, cancellationToken);
        UserAreaAccessMapper.ApplyChanges(access, model);
    }

    public async Task UpdateRoleAreaAccessAsync(RoleAreaAccess access, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.RoleAreaAccesses.FirstAsync(x => x.Id == access.Id, cancellationToken);
        RoleAreaAccessMapper.ApplyChanges(access, model);
    }

    public async Task<IReadOnlyCollection<UserAreaAccess>> ListUserAreaAccessesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.UserAreaAccesses
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

        return models.Select(UserAreaAccessMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyCollection<RoleAreaAccess>> ListRoleAreaAccessesAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }

        var models = await _dbContext.RoleAreaAccesses
            .AsNoTracking()
            .Where(x => roleIds.Contains(x.RoleId))
            .ToListAsync(cancellationToken);

        return models.Select(RoleAreaAccessMapper.ToDomain).ToList();
    }
}
