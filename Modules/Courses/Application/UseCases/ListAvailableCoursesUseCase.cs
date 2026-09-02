using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Courses.Application.DTOs;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class ListAvailableCoursesUseCase
{
    private readonly CourseAccessService _courseAccessService;

    public ListAvailableCoursesUseCase(CourseAccessService courseAccessService)
    {
        _courseAccessService = courseAccessService;
    }

    public async Task<CourseCatalogOutput> ExecuteAsync(
        ListAvailableCoursesInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(input));
        }

        var areas = await _courseAccessService.ListActiveAreasAsync(cancellationToken);
        var catalogEntries = await _courseAccessService.ListCatalogAsync(input.UserId, cancellationToken);

        if (input.HasAccess is not null)
        {
            catalogEntries = catalogEntries
                .Where(entry => entry.HasAccess == input.HasAccess)
                .ToList();
        }

        return new CourseCatalogOutput
        {
            Areas = areas
                .OrderBy(area => area.DisplayOrder)
                .ThenBy(area => area.Name)
                .Select(AreaSummaryOutput.FromAreaOutput)
                .ToList(),
            Courses = catalogEntries
                .OrderBy(entry => entry.Course.DisplayOrder)
                .Select(CourseCatalogItemOutput.FromCatalogEntry)
                .ToList()
        };
    }
}
