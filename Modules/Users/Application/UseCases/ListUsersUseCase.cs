using CourseCore.Api.Modules.Users.Application.DTOs;
using CourseCore.Api.Modules.Users.Domain.Repositories;

namespace CourseCore.Api.Modules.Users.Application.UseCases;

public class ListUsersUseCase
{
    private readonly IUserRepository _users;

    public ListUsersUseCase(IUserRepository users)
    {
        _users = users;
    }

    public async Task<IReadOnlyCollection<UserOutput>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _users.ListAsync(cancellationToken);

        return users.Select(UserOutput.FromUser).ToList();
    }
}
