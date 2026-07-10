using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;

public class CourseAreaPersistenceModel
{
    public Guid CourseId { get; set; }

    public Guid AreaId { get; set; }

    public DateTime CreatedAt { get; set; }

    public CoursePersistenceModel? Course { get; set; }

    public AreaPersistenceModel? Area { get; set; }
}
