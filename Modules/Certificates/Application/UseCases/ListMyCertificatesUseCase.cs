using CourseCore.Api.Modules.Certificates.Application.DTOs;
using CourseCore.Api.Modules.Certificates.Domain.Repositories;
using CourseCore.Api.Modules.Courses.Domain.Repositories;

namespace CourseCore.Api.Modules.Certificates.Application.UseCases;

public class ListMyCertificatesUseCase
{
    private readonly ICertificateRepository _certificates;
    private readonly ICourseRepository _courses;

    public ListMyCertificatesUseCase(ICertificateRepository certificates, ICourseRepository courses)
    {
        _certificates = certificates;
        _courses = courses;
    }

    public async Task<IReadOnlyCollection<CertificateOutput>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var certificates = await _certificates.ListByUserIdAsync(userId, cancellationToken);
        var courseIds = certificates.Select(certificate => certificate.CourseId).Distinct().ToList();
        var courses = await _courses.ListByIdsAsync(courseIds, cancellationToken);
        var coursesById = courses.ToDictionary(course => course.Id);

        return certificates
            .OrderByDescending(certificate => certificate.IssuedAt)
            .Select(certificate =>
            {
                coursesById.TryGetValue(certificate.CourseId, out var course);

                return new CertificateOutput
                {
                    Id = certificate.Id,
                    CourseId = certificate.CourseId,
                    CourseTitle = course?.Title ?? string.Empty,
                    CourseSlug = course?.Slug.Value ?? string.Empty,
                    IssuedAt = certificate.IssuedAt
                };
            })
            .ToList();
    }
}
