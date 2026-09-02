namespace CourseCore.Api.Modules.Courses.Presentation.Responses;

public class CourseCatalogResponse
{
    public IReadOnlyCollection<AreaSummaryResponse> Areas { get; init; } = Array.Empty<AreaSummaryResponse>();

    public IReadOnlyCollection<CourseCatalogItemResponse> Courses { get; init; } = Array.Empty<CourseCatalogItemResponse>();
}
