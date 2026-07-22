using CourseCore.Api.Modules.Users.Domain.Entities;

namespace CourseCore.Api.Modules.Auth.Application.Contracts;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(
        User user,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default);
}
