using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.AuditLogs.Domain.Entities;

public class AuditLog : EntityBase
{
    private AuditLog(Guid? userId, string action, string entityName, Guid? entityId, string? metadataJson)
    {
        UserId = userId;
        Action = ValidateRequired(action, nameof(Action));
        EntityName = ValidateRequired(entityName, nameof(EntityName));
        EntityId = entityId;
        MetadataJson = NormalizeOptional(metadataJson);
    }

    public Guid? UserId { get; private set; }

    public string Action { get; private set; }

    public string EntityName { get; private set; }

    public Guid? EntityId { get; private set; }

    public string? MetadataJson { get; private set; }

    public static AuditLog Create(Guid? userId, string action, string entityName, Guid? entityId, string? metadataJson = null)
    {
        return new AuditLog(userId, action, entityName, entityId, metadataJson);
    }

    public static AuditLog Restore(
        Guid id,
        Guid? userId,
        string action,
        string entityName,
        Guid? entityId,
        string? metadataJson,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new AuditLog(userId, action, entityName, entityId, metadataJson)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void ChangeMetadata(string? metadataJson)
    {
        MetadataJson = NormalizeOptional(metadataJson);
        MarkAsUpdated();
    }

    private static string ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
