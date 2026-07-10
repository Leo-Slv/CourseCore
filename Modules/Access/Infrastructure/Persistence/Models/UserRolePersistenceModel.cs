using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

public class UserRolePersistenceModel
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public DateTime CreatedAt { get; set; }

    public UserPersistenceModel? User { get; set; }

    public RolePersistenceModel? Role { get; set; }
}
