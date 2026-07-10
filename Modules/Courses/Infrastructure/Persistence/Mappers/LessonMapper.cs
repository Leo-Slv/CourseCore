using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Mappers;

public static class LessonMapper
{
    public static Lesson ToDomain(LessonPersistenceModel model)
    {
        return Lesson.Restore(
            model.Id,
            model.ModuleId,
            model.Title,
            model.Description,
            model.DisplayOrder,
            model.FreePreview,
            model.Published,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static LessonPersistenceModel ToPersistence(Lesson lesson)
    {
        return new LessonPersistenceModel
        {
            Id = lesson.Id,
            ModuleId = lesson.ModuleId,
            Title = lesson.Title,
            Description = lesson.Description,
            DisplayOrder = lesson.DisplayOrder,
            FreePreview = lesson.FreePreview,
            Published = lesson.Published,
            CreatedAt = lesson.CreatedAt,
            UpdatedAt = lesson.UpdatedAt
        };
    }

    public static void ApplyChanges(Lesson lesson, LessonPersistenceModel model)
    {
        model.ModuleId = lesson.ModuleId;
        model.Title = lesson.Title;
        model.Description = lesson.Description;
        model.DisplayOrder = lesson.DisplayOrder;
        model.FreePreview = lesson.FreePreview;
        model.Published = lesson.Published;
        model.UpdatedAt = lesson.UpdatedAt;
    }
}
