using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;
using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Models;

public class UserLessonProgressPersistenceModel
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid LessonId { get; set; }

    public bool Completed { get; set; }

    public int WatchedSeconds { get; set; }

    public DateTime LastWatchedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public UserPersistenceModel? User { get; set; }

    public LessonPersistenceModel? Lesson { get; set; }
}
