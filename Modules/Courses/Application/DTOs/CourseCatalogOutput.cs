namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class CourseCatalogOutput
{
    public IReadOnlyCollection<AreaSummaryOutput> Areas { get; init; } = Array.Empty<AreaSummaryOutput>();

    public IReadOnlyCollection<CourseCatalogItemOutput> Courses { get; init; } = Array.Empty<CourseCatalogItemOutput>();
}
