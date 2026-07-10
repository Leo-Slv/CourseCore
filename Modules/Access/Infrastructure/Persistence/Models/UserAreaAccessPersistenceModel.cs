using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

public class UserAreaAccessPersistenceModel
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid AreaId { get; set; }

    public bool CanView { get; set; }

    public bool CanManage { get; set; }

    public DateTime? StartsAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public UserPersistenceModel? User { get; set; }

    public AreaPersistenceModel? Area { get; set; }
}
