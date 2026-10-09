using CourseCore.Api.Modules.Access.Domain.Entities;

namespace CourseCore.Api.Modules.Access.Domain.Repositories;

public interface IUserCourseAccessRepository
{
    Task<UserCourseAccess?> FindAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserCourseAccess>> ListByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<string>>> FindGrantedCourseTitlesByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);

    Task CreateAsync(UserCourseAccess access, CancellationToken cancellationToken = default);

    Task RemoveAsync(UserCourseAccess access, CancellationToken cancellationToken = default);
}
