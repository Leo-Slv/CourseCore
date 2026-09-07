namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class PublicCatalogSummaryOutput
{
    public int ActiveAreaCount { get; init; }

    public int PublishedCourseCount { get; init; }

    public IReadOnlyCollection<PublicFeaturedCourseOutput> FeaturedCourses { get; init; }
        = Array.Empty<PublicFeaturedCourseOutput>();

    public PublicFeaturedCourseOutput? HighlightedCourse { get; init; }

    public IReadOnlyCollection<PublicAreaSummaryOutput> Areas { get; init; }
        = Array.Empty<PublicAreaSummaryOutput>();
}
