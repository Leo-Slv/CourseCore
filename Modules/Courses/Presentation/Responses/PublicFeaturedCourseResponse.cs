namespace CourseCore.Api.Modules.Courses.Presentation.Responses;

public class PublicFeaturedCourseResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? ThumbnailUrl { get; init; }

    public string PricingModel { get; init; } = string.Empty;

    public decimal? PriceAmount { get; init; }

    public int ModuleCount { get; init; }

    public int LessonCount { get; init; }

    public int DurationSeconds { get; init; }

    public string? AreaName { get; init; }
}
