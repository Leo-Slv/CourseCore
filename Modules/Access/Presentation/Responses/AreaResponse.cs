namespace CourseCore.Api.Modules.Access.Presentation.Responses;

public class AreaResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool Active { get; init; }

    public int DisplayOrder { get; init; }

    public string AccentColor { get; init; } = string.Empty;

    public int CourseCount { get; init; }

    public IReadOnlyCollection<AreaCourseSummaryResponse> Courses { get; init; } = Array.Empty<AreaCourseSummaryResponse>();

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
