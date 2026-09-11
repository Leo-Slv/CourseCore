namespace CourseCore.Api.Modules.Progress.Application.DTOs;

public class UpsertLessonNoteInput
{
    public Guid UserId { get; init; }

    public Guid LessonId { get; init; }

    public string Content { get; init; } = string.Empty;
}
