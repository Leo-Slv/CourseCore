namespace CourseCore.Api.Modules.Progress.Presentation.Responses;

public class LessonProgressResponse
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public Guid LessonId { get; init; }

    public bool Completed { get; init; }

    public int WatchedSeconds { get; init; }

    public DateTime LastWatchedAt { get; init; }

    public DateTime? CompletedAt { get; init; }
}
