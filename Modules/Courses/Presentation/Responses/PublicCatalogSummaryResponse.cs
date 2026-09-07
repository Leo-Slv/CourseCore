namespace CourseCore.Api.Modules.Courses.Presentation.Responses;

public class PublicCatalogSummaryResponse
{
    public int ActiveAreaCount { get; init; }

    public int PublishedCourseCount { get; init; }

    public IReadOnlyCollection<PublicFeaturedCourseResponse> FeaturedCourses { get; init; }
        = Array.Empty<PublicFeaturedCourseResponse>();

    public PublicFeaturedCourseResponse? HighlightedCourse { get; init; }

    public IReadOnlyCollection<PublicAreaSummaryResponse> Areas { get; init; }
        = Array.Empty<PublicAreaSummaryResponse>();
}
