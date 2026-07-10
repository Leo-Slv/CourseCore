namespace CourseCore.Api.Modules.Courses.Presentation.Requests;

public class CreateLessonRequest
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool FreePreview { get; init; }
}
