namespace CourseCore.Api.Modules.Courses.Presentation.Responses;

public class CourseModuleResponse
{
    public Guid Id { get; init; }

    public Guid CourseId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool Published { get; init; }

    public IReadOnlyCollection<LessonResponse> Lessons { get; init; } = Array.Empty<LessonResponse>();
}
