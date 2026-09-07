namespace CourseCore.Api.Modules.Courses.Presentation.Responses;

public class PublicAreaSummaryResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public int PublishedCourseCount { get; init; }
}
