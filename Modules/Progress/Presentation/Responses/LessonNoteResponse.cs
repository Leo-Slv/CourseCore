namespace CourseCore.Api.Modules.Progress.Presentation.Responses;

public class LessonNoteResponse
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public Guid LessonId { get; init; }

    public string Content { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
