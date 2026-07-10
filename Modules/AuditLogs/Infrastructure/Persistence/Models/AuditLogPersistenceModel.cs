using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.AuditLogs.Infrastructure.Persistence.Models;

public class AuditLogPersistenceModel
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public UserPersistenceModel? User { get; set; }
}
