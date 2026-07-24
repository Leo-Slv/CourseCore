namespace CourseCore.Api.Modules.AuditLogs.Application.Services;

public interface IAuditLogService
{
    Task RecordAsync(
        string action,
        string entityName,
        Guid? entityId = null,
        IReadOnlyDictionary<string, string?>? metadata = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}
