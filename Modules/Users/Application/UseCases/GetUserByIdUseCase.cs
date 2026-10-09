using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Users.Application.DTOs;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Users.Application.UseCases;

public class GetUserByIdUseCase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IUserCourseAccessRepository _courseAccesses;

    public GetUserByIdUseCase(IUserRepository users, IRoleRepository roles, IUserCourseAccessRepository courseAccesses)
    {
        _users = users;
        _roles = roles;
        _courseAccesses = courseAccesses;
    }

    public async Task<UserOutput> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        var roles = await _roles.FindByUserIdAsync(userId, cancellationToken);
        var courseTitlesByUserId = await _courseAccesses.FindGrantedCourseTitlesByUserIdsAsync([userId], cancellationToken);

        return UserOutput.FromUser(
            user,
            roles.Select(role => role.Name).ToList(),
            courseTitlesByUserId.TryGetValue(userId, out var courseTitles) ? courseTitles : null);
    }
}
