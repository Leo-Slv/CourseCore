namespace CourseCore.Api.Modules.Courses.Presentation.Requests;

public class UpdateCourseModuleRequest
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool Published { get; init; }
}
