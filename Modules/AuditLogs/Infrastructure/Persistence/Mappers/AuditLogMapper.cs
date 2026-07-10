using CourseCore.Api.Modules.AuditLogs.Domain.Entities;
using CourseCore.Api.Modules.AuditLogs.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.AuditLogs.Infrastructure.Persistence.Mappers;

public static class AuditLogMapper
{
    public static AuditLog ToDomain(AuditLogPersistenceModel model)
    {
        return AuditLog.Restore(
            model.Id,
            model.UserId,
            model.Action,
            model.EntityName,
            model.EntityId,
            model.MetadataJson,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static AuditLogPersistenceModel ToPersistence(AuditLog auditLog)
    {
        return new AuditLogPersistenceModel
        {
            Id = auditLog.Id,
            UserId = auditLog.UserId,
            Action = auditLog.Action,
            EntityName = auditLog.EntityName,
            EntityId = auditLog.EntityId,
            MetadataJson = auditLog.MetadataJson,
            CreatedAt = auditLog.CreatedAt,
            UpdatedAt = auditLog.UpdatedAt
        };
    }

    public static void ApplyChanges(AuditLog auditLog, AuditLogPersistenceModel model)
    {
        model.UserId = auditLog.UserId;
        model.Action = auditLog.Action;
        model.EntityName = auditLog.EntityName;
        model.EntityId = auditLog.EntityId;
        model.MetadataJson = auditLog.MetadataJson;
        model.UpdatedAt = auditLog.UpdatedAt;
    }
}
