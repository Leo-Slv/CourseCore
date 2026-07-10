namespace CourseCore.Api.Modules.Progress.Presentation.Responses;

public class CourseProgressResponse
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public Guid CourseId { get; init; }

    public decimal ProgressPercent { get; init; }

    public DateTime StartedAt { get; init; }

    public DateTime? CompletedAt { get; init; }

    public IReadOnlyCollection<LessonProgressResponse> Lessons { get; init; } = Array.Empty<LessonProgressResponse>();
}
