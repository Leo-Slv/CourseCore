using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Users.Infrastructure.Persistence.Models;

public class UserPersistenceModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool Active { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<UserRolePersistenceModel> UserRoles { get; set; } = new List<UserRolePersistenceModel>();

    public ICollection<UserAreaAccessPersistenceModel> AreaAccesses { get; set; } = new List<UserAreaAccessPersistenceModel>();

    public ICollection<UserCourseProgressPersistenceModel> CourseProgresses { get; set; } = new List<UserCourseProgressPersistenceModel>();

    public ICollection<UserLessonProgressPersistenceModel> LessonProgresses { get; set; } = new List<UserLessonProgressPersistenceModel>();
}
