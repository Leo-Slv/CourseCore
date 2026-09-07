using CourseCore.Api.Modules.Certificates.Domain.Entities;

namespace CourseCore.Api.Modules.Certificates.Domain.Repositories;

public interface ICertificateRepository
{
    Task<Certificate?> FindByUserAndCourseAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Certificate>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, Certificate>> ListByUserIdAndCourseIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> courseIds,
        CancellationToken cancellationToken = default);

    Task CreateAsync(Certificate certificate, CancellationToken cancellationToken = default);
}
