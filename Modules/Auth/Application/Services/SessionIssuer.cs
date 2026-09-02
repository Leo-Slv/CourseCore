using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Modules.Users.Domain.Entities;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Auth.Application.Services;

public class SessionIssuer
{
    private readonly IRoleRepository _roles;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly JwtOptions _jwtOptions;

    public SessionIssuer(
        IRoleRepository roles,
        ITokenService tokenService,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IOptions<JwtOptions> jwtOptions)
    {
        _roles = roles;
        _tokenService = tokenService;
        _refreshTokenGenerator = refreshTokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<SessionIssueResult> BuildAsync(User user, CancellationToken cancellationToken = default)
    {
        var roles = await _roles.FindByUserIdAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(role => role.Name).ToArray();
        var permissions = await _roles.FindPermissionKeysByUserIdAsync(user.Id, cancellationToken);
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user, roleNames, permissions, cancellationToken);
        var refreshTokenValue = _refreshTokenGenerator.Generate();
        var refreshTokenHash = _refreshTokenHasher.Hash(refreshTokenValue);
        var now = DateTime.UtcNow;

        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenHash,
            now.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            now);

        var output = new AuthOutput
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email.Value,
            Roles = roleNames,
            Token = new AuthToken
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                ExpiresAt = now.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes)
            }
        };

        return new SessionIssueResult(output, refreshToken);
    }
}
