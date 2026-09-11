namespace CourseCore.Api.Modules.Media.Presentation.Requests;

public class UpdateLessonMaterialRequest
{
    public string Title { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }
}
