using CourseCore.Api.Modules.Users.Domain.Entities;

namespace CourseCore.Api.Modules.Auth.Application.Contracts;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken = default);
}
