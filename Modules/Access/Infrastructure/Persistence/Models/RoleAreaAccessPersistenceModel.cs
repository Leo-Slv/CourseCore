namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

public class RoleAreaAccessPersistenceModel
{
    public Guid Id { get; set; }

    public Guid RoleId { get; set; }

    public Guid AreaId { get; set; }

    public bool CanView { get; set; }

    public bool CanManage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public RolePersistenceModel? Role { get; set; }

    public AreaPersistenceModel? Area { get; set; }
}
