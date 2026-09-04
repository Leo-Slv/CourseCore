using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Enums;

namespace CourseCore.Api.Modules.Access.Domain.Repositories;

public interface IAccessRequestRepository
{
    Task<AccessRequest?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AccessRequest?> FindPendingByUserAndCourseAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccessRequest>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccessRequest>> ListAsync(
        AccessRequestStatus? status,
        CancellationToken cancellationToken = default);

    Task CreateAsync(AccessRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(AccessRequest request, CancellationToken cancellationToken = default);
}
