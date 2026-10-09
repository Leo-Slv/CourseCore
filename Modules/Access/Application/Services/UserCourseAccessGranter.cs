using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Modules.Access.Domain.Repositories;

namespace CourseCore.Api.Modules.Access.Application.Services;

public class UserCourseAccessGranter
{
    private readonly IUserCourseAccessRepository _accesses;

    public UserCourseAccessGranter(IUserCourseAccessRepository accesses)
    {
        _accesses = accesses;
    }

    /// <summary>
    /// Idempotent. Returns true when a new grant was created, false when the user already had one.
    /// </summary>
    public async Task<bool> EnsureGrantedAsync(
        Guid userId,
        Guid courseId,
        CourseAccessSource source,
        CancellationToken cancellationToken = default)
    {
        if (await _accesses.ExistsAsync(userId, courseId, cancellationToken))
        {
            return false;
        }

        await _accesses.CreateAsync(UserCourseAccess.Create(userId, courseId, source), cancellationToken);

        return true;
    }
}
