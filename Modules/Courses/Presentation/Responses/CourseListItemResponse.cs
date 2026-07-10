namespace CourseCore.Api.Modules.Courses.Presentation.Responses;

public class CourseListItemResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? ThumbnailUrl { get; init; }

    public int DisplayOrder { get; init; }
}
