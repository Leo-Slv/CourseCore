namespace CourseCore.Api.Modules.AuditLogs.Presentation.Responses;

public class AuditLogResponse
{
    public Guid Id { get; init; }

    public Guid? UserId { get; init; }

    public string Action { get; init; } = string.Empty;

    public string EntityName { get; init; } = string.Empty;

    public Guid? EntityId { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    public DateTime CreatedAt { get; init; }
}
