using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Repositories;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class ListUserCourseAccessUseCase
{
    private readonly IUserCourseAccessRepository _accesses;

    public ListUserCourseAccessUseCase(IUserCourseAccessRepository accesses)
    {
        _accesses = accesses;
    }

    public async Task<IReadOnlyCollection<UserCourseAccessOutput>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var accesses = await _accesses.ListByUserIdAsync(userId, cancellationToken);

        return accesses.Select(UserCourseAccessOutput.FromUserCourseAccess).ToList();
    }
}
