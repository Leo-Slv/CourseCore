namespace CourseCore.Api.Modules.Courses.Presentation.Responses;

public class AreaSummaryResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }
}
