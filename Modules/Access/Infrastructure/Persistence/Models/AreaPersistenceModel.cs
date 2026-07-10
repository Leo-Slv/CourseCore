using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

public class AreaPersistenceModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool Active { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<CourseAreaPersistenceModel> CourseAreas { get; set; } = new List<CourseAreaPersistenceModel>();

    public ICollection<UserAreaAccessPersistenceModel> UserAccesses { get; set; } = new List<UserAreaAccessPersistenceModel>();

    public ICollection<RoleAreaAccessPersistenceModel> RoleAccesses { get; set; } = new List<RoleAreaAccessPersistenceModel>();
}
