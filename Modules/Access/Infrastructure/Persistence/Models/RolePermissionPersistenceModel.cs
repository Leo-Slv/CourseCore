namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

public class RolePermissionPersistenceModel
{
    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public RolePersistenceModel? Role { get; set; }

    public PermissionPersistenceModel? Permission { get; set; }
}
