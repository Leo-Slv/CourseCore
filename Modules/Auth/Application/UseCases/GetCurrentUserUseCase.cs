using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Auth.Application.UseCases;

public class GetCurrentUserUseCase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;

    public GetCurrentUserUseCase(IUserRepository users, IRoleRepository roles)
    {
        _users = users;
        _roles = roles;
    }

    public async Task<CurrentUserOutput> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var user = await _users.FindByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        var roles = await _roles.FindByUserIdAsync(userId, cancellationToken);

        return new CurrentUserOutput
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email.Value,
            Active = user.Active,
            EmailVerifiedAt = user.EmailVerifiedAt,
            Roles = roles.Select(role => role.Name).ToList()
        };
    }
}
