namespace CourseCore.Api.Modules.Courses.Presentation.Requests;

public class UpdateLessonRequest
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool FreePreview { get; init; }

    public bool Published { get; init; }
}
