using CourseCore.Api.Modules.AuditLogs.Domain.Entities;
using CourseCore.Api.Modules.AuditLogs.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.AuditLogs.Infrastructure.Persistence.Repositories;

public class EfAuditLogRepository : IAuditLogRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfAuditLogRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditLogs.AddAsync(AuditLogMapper.ToPersistence(auditLog), cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditLog>> ListByEntityAsync(
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.EntityName == entityName && x.EntityId == entityId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return models.Select(AuditLogMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyCollection<AuditLog>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var models = await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return models.Select(AuditLogMapper.ToDomain).ToList();
    }
}
