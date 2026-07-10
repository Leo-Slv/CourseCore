namespace CourseCore.Api.Modules.Progress.Application.DTOs;

public class RegisterLessonProgressInput
{
    public Guid UserId { get; init; }

    public Guid LessonId { get; init; }

    public int WatchedSeconds { get; init; }

    public bool MarkAsCompleted { get; init; }
}
