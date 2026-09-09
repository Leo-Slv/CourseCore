using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Users.Application.DTOs;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Users.Application.UseCases;

public class GetUserByIdUseCase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IAreaRepository _areas;

    public GetUserByIdUseCase(IUserRepository users, IRoleRepository roles, IAreaRepository areas)
    {
        _users = users;
        _roles = roles;
        _areas = areas;
    }

    public async Task<UserOutput> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        var roles = await _roles.FindByUserIdAsync(userId, cancellationToken);
        var areaNamesByUserId = await _areas.FindGrantedAreaNamesByUserIdsAsync([userId], cancellationToken);

        return UserOutput.FromUser(
            user,
            roles.Select(role => role.Name).ToList(),
            areaNamesByUserId.TryGetValue(userId, out var areaNames) ? areaNames : null);
    }
}
