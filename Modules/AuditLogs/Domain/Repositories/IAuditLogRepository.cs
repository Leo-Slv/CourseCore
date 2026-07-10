using CourseCore.Api.Modules.AuditLogs.Domain.Entities;

namespace CourseCore.Api.Modules.AuditLogs.Domain.Repositories;

public interface IAuditLogRepository
{
    Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AuditLog>> ListByEntityAsync(
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AuditLog>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
