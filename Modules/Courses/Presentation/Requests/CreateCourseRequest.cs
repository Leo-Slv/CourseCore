namespace CourseCore.Api.Modules.Courses.Presentation.Requests;

public class CreateCourseRequest
{
    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? ThumbnailUrl { get; init; }

    public int DisplayOrder { get; init; }

    public IReadOnlyCollection<Guid> AreaIds { get; init; } = Array.Empty<Guid>();

    public IReadOnlyCollection<CreateCourseModuleRequest> Modules { get; init; } = Array.Empty<CreateCourseModuleRequest>();
}
