using System.Text.Json;
using CourseCore.Api.Modules.AuditLogs.Domain.Entities;

namespace CourseCore.Api.Modules.AuditLogs.Application.DTOs;

public class AuditLogOutput
{
    public Guid Id { get; init; }

    public Guid? UserId { get; init; }

    public string Action { get; init; } = string.Empty;

    public string EntityName { get; init; } = string.Empty;

    public Guid? EntityId { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    public DateTime CreatedAt { get; init; }

    public static AuditLogOutput FromAuditLog(AuditLog auditLog)
    {
        return new AuditLogOutput
        {
            Id = auditLog.Id,
            UserId = auditLog.UserId,
            Action = auditLog.Action,
            EntityName = auditLog.EntityName,
            EntityId = auditLog.EntityId,
            Metadata = ParseMetadata(auditLog.MetadataJson),
            CreatedAt = auditLog.CreatedAt
        };
    }

    private static IReadOnlyDictionary<string, string> ParseMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new Dictionary<string, string>();
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson)
            ?? new Dictionary<string, string>();
    }
}
