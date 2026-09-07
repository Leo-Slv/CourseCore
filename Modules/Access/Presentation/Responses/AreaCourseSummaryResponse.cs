namespace CourseCore.Api.Modules.Access.Presentation.Responses;

public class AreaCourseSummaryResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public bool Published { get; init; }
}
