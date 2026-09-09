using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Media.Domain.Repositories;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class GetPublicCatalogSummaryUseCase
{
    private const int FeaturedCourseCount = 3;

    private readonly ICourseRepository _courses;
    private readonly IAreaRepository _areas;
    private readonly IVideoRepository _videos;

    public GetPublicCatalogSummaryUseCase(ICourseRepository courses, IAreaRepository areas, IVideoRepository videos)
    {
        _courses = courses;
        _areas = areas;
        _videos = videos;
    }

    public async Task<PublicCatalogSummaryOutput> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var areas = await _areas.ListAsync(cancellationToken);
        var activeAreas = areas.Where(area => area.Active).ToList();
        var activeAreasById = activeAreas.ToDictionary(area => area.Id);
        var publishedCourses = await _courses.ListPublishedAsync(cancellationToken);
        var highlightedCourse = publishedCourses.FirstOrDefault(course => course.IsFeatured);

        var featuredCourses = publishedCourses.Take(FeaturedCourseCount).ToList();
        var coursesToEnrich = featuredCourses.ToList();
        if (highlightedCourse is not null && !coursesToEnrich.Any(course => course.Id == highlightedCourse.Id))
        {
            coursesToEnrich.Add(highlightedCourse);
        }

        var courseIds = coursesToEnrich.Select(course => course.Id).ToList();
        var summariesByCourseId = (await _courses.ListContentSummariesAsync(courseIds, cancellationToken))
            .ToDictionary(summary => summary.CourseId);

        var lessonIds = summariesByCourseId.Values
            .SelectMany(summary => summary.LessonIds)
            .Distinct()
            .ToList();
        var durationsByLessonId = await _videos.ListDurationSecondsByLessonIdsAsync(lessonIds, cancellationToken);

        string? ResolveAreaName(Course course)
        {
            return course.AreaIds
                .Where(activeAreasById.ContainsKey)
                .Select(areaId => activeAreasById[areaId])
                .OrderBy(area => area.DisplayOrder)
                .Select(area => area.Name)
                .FirstOrDefault();
        }

        PublicFeaturedCourseOutput BuildOutput(Course course)
        {
            var summary = summariesByCourseId.GetValueOrDefault(course.Id);
            var durationSeconds = summary is null
                ? 0
                : summary.LessonIds.Sum(lessonId => durationsByLessonId.GetValueOrDefault(lessonId, 0));

            return PublicFeaturedCourseOutput.FromCourse(
                course,
                summary?.ModuleCount ?? 0,
                summary?.LessonCount ?? 0,
                durationSeconds,
                ResolveAreaName(course));
        }

        return new PublicCatalogSummaryOutput
        {
            ActiveAreaCount = activeAreas.Count,
            PublishedCourseCount = publishedCourses.Count,
            FeaturedCourses = featuredCourses.Select(BuildOutput).ToList(),
            HighlightedCourse = highlightedCourse is null ? null : BuildOutput(highlightedCourse),
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
