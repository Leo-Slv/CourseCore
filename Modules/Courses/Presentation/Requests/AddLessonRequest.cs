namespace CourseCore.Api.Modules.Courses.Presentation.Requests;

public class AddLessonRequest
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool FreePreview { get; init; }
}
