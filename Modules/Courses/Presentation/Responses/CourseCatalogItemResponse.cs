namespace CourseCore.Api.Modules.Courses.Presentation.Responses;

public class CourseCatalogItemResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? ThumbnailUrl { get; init; }

    public int DisplayOrder { get; init; }

    public string PricingModel { get; init; } = string.Empty;

    public IReadOnlyCollection<Guid> AreaIds { get; init; } = Array.Empty<Guid>();

    public bool HasAccess { get; init; }

    public int ModuleCount { get; init; }

    public int LessonCount { get; init; }

    public int DurationSeconds { get; init; }
}
