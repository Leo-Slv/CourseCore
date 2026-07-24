using System.Text.Json;
using CourseCore.Api.Modules.AuditLogs.Domain.Entities;
using CourseCore.Api.Modules.AuditLogs.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;

namespace CourseCore.Api.Modules.AuditLogs.Application.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogs;
    private readonly ICurrentUserService _currentUser;
    private readonly IRequestContextService _requestContext;

    public AuditLogService(
        IAuditLogRepository auditLogs,
        ICurrentUserService currentUser,
        IRequestContextService requestContext)
    {
        _auditLogs = auditLogs;
        _currentUser = currentUser;
        _requestContext = requestContext;
    }

    public Task RecordAsync(
        string action,
        string entityName,
        Guid? entityId = null,
        IReadOnlyDictionary<string, string?>? metadata = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var metadataJson = BuildMetadataJson(metadata);
        var auditLog = AuditLog.Create(userId ?? _currentUser.UserId, action, entityName, entityId, metadataJson);

        return _auditLogs.CreateAsync(auditLog, cancellationToken);
    }

    private string? BuildMetadataJson(IReadOnlyDictionary<string, string?>? metadata)
    {
        var sanitized = new SortedDictionary<string, string>(StringComparer.Ordinal);

        if (metadata is not null)
        {
            foreach (var (key, value) in metadata)
            {
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                sanitized[key.Trim()] = value.Trim();
            }
        }

        if (!string.IsNullOrWhiteSpace(_requestContext.CorrelationId))
        {
            sanitized["correlationId"] = _requestContext.CorrelationId.Trim();
        }

        return sanitized.Count == 0 ? null : JsonSerializer.Serialize(sanitized);
    }
}
