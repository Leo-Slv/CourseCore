namespace CourseCore.Api.Modules.Progress.Presentation.Requests;

public class RegisterLessonProgressRequest
{
    public Guid UserId { get; init; }

    public Guid LessonId { get; init; }

    public int WatchedSeconds { get; init; }

    public bool MarkAsCompleted { get; init; }
}
