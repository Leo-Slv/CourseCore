namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class UpdateLessonInput
{
    public Guid LessonId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool FreePreview { get; init; }

    public bool Published { get; init; }
}
