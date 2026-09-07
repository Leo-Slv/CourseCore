using CourseCore.Api.Modules.Certificates.Domain.Entities;
using CourseCore.Api.Modules.Certificates.Domain.Repositories;

namespace CourseCore.Api.Tests.TestDoubles;

public sealed class FakeCertificateRepository : ICertificateRepository
{
    public List<Certificate> CreatedCertificates { get; } = [];

    public Task<Certificate?> FindByUserAndCourseAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var certificate = CreatedCertificates.FirstOrDefault(
            certificate => certificate.UserId == userId && certificate.CourseId == courseId);

        return Task.FromResult(certificate);
    }

    public Task<IReadOnlyCollection<Certificate>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Certificate>>(
            CreatedCertificates.Where(certificate => certificate.UserId == userId).ToArray());
    }

    public Task<IReadOnlyDictionary<Guid, Certificate>> ListByUserIdAndCourseIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default)
    {
        var courseIdSet = courseIds.ToHashSet();
        IReadOnlyDictionary<Guid, Certificate> result = CreatedCertificates
            .Where(certificate => certificate.UserId == userId && courseIdSet.Contains(certificate.CourseId))
            .ToDictionary(certificate => certificate.CourseId);

        return Task.FromResult(result);
    }

    public Task CreateAsync(Certificate certificate, CancellationToken cancellationToken = default)
    {
        CreatedCertificates.Add(certificate);

        return Task.CompletedTask;
    }
}
