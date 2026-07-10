namespace CourseCore.Api.Modules.Courses.Presentation.Responses;

public class CourseDetailsResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? ThumbnailUrl { get; init; }

    public bool Published { get; init; }

    public int DisplayOrder { get; init; }

    public DateTime? PublishedAt { get; init; }

    public IReadOnlyCollection<Guid> AreaIds { get; init; } = Array.Empty<Guid>();

    public IReadOnlyCollection<CourseModuleResponse> Modules { get; init; } = Array.Empty<CourseModuleResponse>();

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
