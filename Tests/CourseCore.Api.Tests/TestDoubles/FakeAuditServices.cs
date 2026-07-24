using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.AuditLogs.Domain.Entities;
using CourseCore.Api.Modules.AuditLogs.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;

namespace CourseCore.Api.Tests.TestDoubles;

public sealed class FakeAuditLogService : IAuditLogService
{
    public List<AuditLogEntry> Entries { get; } = [];

    public Task RecordAsync(
        string action,
        string entityName,
        Guid? entityId = null,
        IReadOnlyDictionary<string, string?>? metadata = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        Entries.Add(new AuditLogEntry(
            action,
            entityName,
            entityId,
            metadata is null
                ? new Dictionary<string, string?>()
                : new Dictionary<string, string?>(metadata),
            userId));

        return Task.CompletedTask;
    }

    public void Clear()
    {
        Entries.Clear();
    }
}

public sealed record AuditLogEntry(
    string Action,
    string EntityName,
    Guid? EntityId,
    IReadOnlyDictionary<string, string?> Metadata,
    Guid? UserId);

public sealed class FakeAuditLogRepository : IAuditLogRepository
{
    public List<AuditLog> AuditLogs { get; } = [];

    public Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        AuditLogs.Add(auditLog);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<AuditLog>> ListByEntityAsync(
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<AuditLog>>(
            AuditLogs.Where(log => log.EntityName == entityName && log.EntityId == entityId).ToArray());
    }

    public Task<IReadOnlyCollection<AuditLog>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<AuditLog>>(
            AuditLogs.Where(log => log.UserId == userId).ToArray());
    }
}

public sealed class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; set; }

    public string? Email { get; set; }

    public IReadOnlyCollection<string> Roles { get; set; } = [];

    public bool IsAuthenticated { get; set; }
}

public sealed class FakeRequestContextService : IRequestContextService
{
    public string? CorrelationId { get; set; }
}
