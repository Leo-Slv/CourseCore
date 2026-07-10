using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Models;

public class UserCourseProgressPersistenceModel
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CourseId { get; set; }

    public decimal ProgressPercent { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public UserPersistenceModel? User { get; set; }

    public CoursePersistenceModel? Course { get; set; }
}
