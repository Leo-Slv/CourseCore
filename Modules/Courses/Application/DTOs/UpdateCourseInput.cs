namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class UpdateCourseInput
{
    public Guid CourseId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? ThumbnailUrl { get; init; }

    public int DisplayOrder { get; init; }

    public IReadOnlyCollection<Guid> AreaIds { get; init; } = Array.Empty<Guid>();
}
