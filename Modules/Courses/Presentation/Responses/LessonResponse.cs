namespace CourseCore.Api.Modules.Courses.Presentation.Responses;

public class LessonResponse
{
    public Guid Id { get; init; }

    public Guid ModuleId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool FreePreview { get; init; }

    public bool Published { get; init; }
}
