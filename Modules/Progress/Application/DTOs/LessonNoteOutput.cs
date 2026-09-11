using CourseCore.Api.Modules.Progress.Domain.Entities;

namespace CourseCore.Api.Modules.Progress.Application.DTOs;

public class LessonNoteOutput
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public Guid LessonId { get; init; }

    public string Content { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public static LessonNoteOutput FromNote(LessonNote note)
    {
        return new LessonNoteOutput
        {
            Id = note.Id,
            UserId = note.UserId,
            LessonId = note.LessonId,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }
}
