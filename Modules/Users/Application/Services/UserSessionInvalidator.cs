using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;

namespace CourseCore.Api.Modules.Users.Application.Services;

/// <summary>
/// Forces users to sign in again so JWT role/permission claims are re-issued
/// after their roles or role permissions change. Must run inside a unit of work.
/// </summary>
public class UserSessionInvalidator
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;

    public UserSessionInvalidator(IUserRepository users, IRefreshTokenRepository refreshTokens)
    {
        _users = users;
        _refreshTokens = refreshTokens;
    }

    public async Task InvalidateAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var userId in userIds.Distinct())
        {
            var user = await _users.FindByIdAsync(userId, cancellationToken);

            if (user is null)
            {
                continue;
            }

            user.IncrementTokenVersion();
            await _refreshTokens.RevokeActiveByUserIdAsync(user.Id, DateTime.UtcNow, cancellationToken);
            await _users.UpdateAsync(user, cancellationToken);
        }
    }
}
