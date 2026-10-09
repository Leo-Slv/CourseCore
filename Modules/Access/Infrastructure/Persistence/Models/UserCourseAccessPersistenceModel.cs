using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

public class UserCourseAccessPersistenceModel
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CourseId { get; set; }

    public int Source { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public UserPersistenceModel? User { get; set; }

    public CoursePersistenceModel? Course { get; set; }
}
