using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Repositories;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class GetPublicCatalogSummaryUseCase
{
    private const int FeaturedCourseCount = 3;

    private readonly ICourseRepository _courses;
    private readonly IAreaRepository _areas;

    public GetPublicCatalogSummaryUseCase(ICourseRepository courses, IAreaRepository areas)
    {
        _courses = courses;
        _areas = areas;
    }

    public async Task<PublicCatalogSummaryOutput> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var areas = await _areas.ListAsync(cancellationToken);
        var activeAreas = areas.Where(area => area.Active).ToList();
        var publishedCourses = await _courses.ListPublishedAsync(cancellationToken);
        var highlightedCourse = publishedCourses.FirstOrDefault(course => course.IsFeatured);

        return new PublicCatalogSummaryOutput
        {
            ActiveAreaCount = activeAreas.Count,
            PublishedCourseCount = publishedCourses.Count,
            FeaturedCourses = publishedCourses
                .Take(FeaturedCourseCount)
                .Select(PublicFeaturedCourseOutput.FromCourse)
                .ToList(),
            HighlightedCourse = highlightedCourse is null ? null : PublicFeaturedCourseOutput.FromCourse(highlightedCourse),
            Areas = activeAreas
                .OrderBy(area => area.DisplayOrder)
                .ThenBy(area => area.Name)
                .Select(area => new PublicAreaSummaryOutput
                {
                    Id = area.Id,
                    Name = area.Name,
                    Slug = area.Slug.Value,
                    PublishedCourseCount = publishedCourses.Count(course => course.AreaIds.Contains(area.Id))
                })
                .ToList()
        };
    }
}
