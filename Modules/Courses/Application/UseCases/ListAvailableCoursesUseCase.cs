using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Certificates.Domain.Repositories;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Media.Domain.Repositories;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class ListAvailableCoursesUseCase
{
    private readonly CourseAccessService _courseAccessService;
    private readonly ICourseRepository _courses;
    private readonly IVideoRepository _videos;
    private readonly ICertificateRepository _certificates;

    public ListAvailableCoursesUseCase(
        CourseAccessService courseAccessService,
        ICourseRepository courses,
        IVideoRepository videos,
        ICertificateRepository certificates)
    {
        _courseAccessService = courseAccessService;
        _courses = courses;
        _videos = videos;
        _certificates = certificates;
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

        var courseIds = catalogEntries.Select(entry => entry.Course.Id).ToList();
        var contentSummaries = await _courses.ListContentSummariesAsync(courseIds, cancellationToken);
        var summariesByCourseId = contentSummaries.ToDictionary(summary => summary.CourseId);

        var lessonIds = contentSummaries
            .SelectMany(summary => summary.LessonIds)
            .Distinct()
            .ToList();
        var durationsByLessonId = await _videos.ListDurationSecondsByLessonIdsAsync(lessonIds, cancellationToken);
        var certificatesByCourseId = await _certificates.ListByUserIdAndCourseIdsAsync(
            input.UserId, courseIds, cancellationToken);

        return new CourseCatalogOutput
        {
            Areas = areas
                .OrderBy(area => area.DisplayOrder)
                .ThenBy(area => area.Name)
                .Select(AreaSummaryOutput.FromAreaOutput)
                .ToList(),
            Courses = catalogEntries
                .OrderBy(entry => entry.Course.DisplayOrder)
                .Select(entry =>
                {
                    var summary = summariesByCourseId[entry.Course.Id];
                    var durationSeconds = summary.LessonIds.Sum(lessonId =>
                        durationsByLessonId.GetValueOrDefault(lessonId, 0));
                    var certificateIssued = certificatesByCourseId.ContainsKey(entry.Course.Id);

                    return CourseCatalogItemOutput.FromCatalogEntry(
                        entry, summary.ModuleCount, summary.LessonCount, durationSeconds, certificateIssued);
                })
                .ToList()
        };
    }
}
