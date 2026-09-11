using CourseCore.Api.Modules.Progress.Domain.Entities;
using CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Mappers;

public static class LessonNoteMapper
{
    public static LessonNote ToDomain(LessonNotePersistenceModel model)
    {
        return LessonNote.Restore(
            model.Id,
            model.UserId,
            model.LessonId,
            model.Content,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static LessonNotePersistenceModel ToPersistence(LessonNote note)
    {
        return new LessonNotePersistenceModel
        {
            Id = note.Id,
            UserId = note.UserId,
            LessonId = note.LessonId,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }

    public static void ApplyChanges(LessonNote note, LessonNotePersistenceModel model)
    {
        model.UserId = note.UserId;
        model.LessonId = note.LessonId;
        model.Content = note.Content;
        model.UpdatedAt = note.UpdatedAt;
    }
}
