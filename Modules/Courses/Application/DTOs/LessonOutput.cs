using CourseCore.Api.Modules.Courses.Domain.Entities;

namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class LessonOutput
{
    public Guid Id { get; init; }

    public Guid ModuleId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool FreePreview { get; init; }

    public bool Published { get; init; }

    public static LessonOutput FromLesson(Lesson lesson)
    {
        return new LessonOutput
        {
            Id = lesson.Id,
            ModuleId = lesson.ModuleId,
            Title = lesson.Title,
            Description = lesson.Description,
            DisplayOrder = lesson.DisplayOrder,
            FreePreview = lesson.FreePreview,
            Published = lesson.Published
        };
    }
}
