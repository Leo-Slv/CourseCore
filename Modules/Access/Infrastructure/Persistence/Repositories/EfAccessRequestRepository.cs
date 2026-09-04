using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Repositories;

public class EfAccessRequestRepository : IAccessRequestRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfAccessRequestRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccessRequest?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.AccessRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return model is null ? null : AccessRequestMapper.ToDomain(model);
    }

    public async Task<AccessRequest?> FindPendingByUserAndCourseAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var pendingStatus = AccessRequestStatus.Pending.ToString();
        var model = await _dbContext.AccessRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.CourseId == courseId && x.Status == pendingStatus,
                cancellationToken);

        return model is null ? null : AccessRequestMapper.ToDomain(model);
    }

    public async Task<IReadOnlyCollection<AccessRequest>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.AccessRequests
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return models.Select(AccessRequestMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyCollection<AccessRequest>> ListAsync(
        AccessRequestStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AccessRequests.AsNoTracking();

        if (status is not null)
        {
            var statusValue = status.Value.ToString();
            query = query.Where(x => x.Status == statusValue);
        }

        var models = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return models.Select(AccessRequestMapper.ToDomain).ToList();
    }

    public async Task CreateAsync(AccessRequest request, CancellationToken cancellationToken = default)
    {
        await _dbContext.AccessRequests.AddAsync(AccessRequestMapper.ToPersistence(request), cancellationToken);
    }

    public async Task UpdateAsync(AccessRequest request, CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.AccessRequests
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Access request not found.");
        }

        AccessRequestMapper.ApplyChanges(request, model);
    }
}
