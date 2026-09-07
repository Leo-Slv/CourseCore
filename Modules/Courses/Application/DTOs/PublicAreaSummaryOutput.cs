namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class PublicAreaSummaryOutput
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public int PublishedCourseCount { get; init; }
}
