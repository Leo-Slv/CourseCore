namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

public class RolePersistenceModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<UserRolePersistenceModel> UserRoles { get; set; } = new List<UserRolePersistenceModel>();

    public ICollection<RolePermissionPersistenceModel> RolePermissions { get; set; } = new List<RolePermissionPersistenceModel>();

    public ICollection<RoleAreaAccessPersistenceModel> AreaAccesses { get; set; } = new List<RoleAreaAccessPersistenceModel>();
}
