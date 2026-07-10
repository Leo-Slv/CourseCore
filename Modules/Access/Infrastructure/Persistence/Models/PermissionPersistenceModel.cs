namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

public class PermissionPersistenceModel
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<RolePermissionPersistenceModel> RolePermissions { get; set; } = new List<RolePermissionPersistenceModel>();
}
