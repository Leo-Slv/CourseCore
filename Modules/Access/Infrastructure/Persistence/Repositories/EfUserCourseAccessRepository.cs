using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Repositories;

public class EfUserCourseAccessRepository : IUserCourseAccessRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfUserCourseAccessRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserCourseAccess?> FindAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.UserCourseAccesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.CourseId == courseId, cancellationToken);

        return model is null ? null : UserCourseAccessMapper.ToDomain(model);
    }

    public Task<bool> ExistsAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserCourseAccesses
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.CourseId == courseId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserCourseAccess>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.UserCourseAccesses
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return models.Select(UserCourseAccessMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> FindGrantedCourseTitlesByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyCollection<string>>();
        }

        var rows = await _dbContext.UserCourseAccesses
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId) && x.Course != null)
            .Select(x => new { x.UserId, CourseTitle = x.Course!.Title })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group.Select(row => row.CourseTitle).ToList());
    }

    public async Task CreateAsync(UserCourseAccess access, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserCourseAccesses.AddAsync(UserCourseAccessMapper.ToPersistence(access), cancellationToken);
    }

    public async Task RemoveAsync(UserCourseAccess access, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.UserCourseAccesses.FirstAsync(x => x.Id == access.Id, cancellationToken);
        _dbContext.UserCourseAccesses.Remove(model);
    }
}
