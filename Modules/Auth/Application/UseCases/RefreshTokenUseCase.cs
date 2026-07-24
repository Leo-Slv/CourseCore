using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Auth.Application.UseCases;

public class RefreshTokenUseCase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<RefreshTokenUseCase> _logger;

    public RefreshTokenUseCase(
        IUserRepository users,
        IRoleRepository roles,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        IRefreshTokenHasher refreshTokenHasher,
        IRefreshTokenGenerator refreshTokenGenerator,
        IUnitOfWork unitOfWork,
        IOptions<JwtOptions> jwtOptions,
        ILogger<RefreshTokenUseCase> logger)
    {
        _users = users;
        _roles = roles;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _refreshTokenHasher = refreshTokenHasher;
        _refreshTokenGenerator = refreshTokenGenerator;
        _unitOfWork = unitOfWork;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<AuthOutput> ExecuteAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Refresh token request rejected because the token was missing.");
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var refreshTokenHash = _refreshTokenHasher.Hash(refreshToken);
        var persistedRefreshToken = await _refreshTokens.FindByTokenHashAsync(
            refreshTokenHash,
            cancellationToken);

        if (persistedRefreshToken is null || !persistedRefreshToken.IsActive)
        {
            _logger.LogWarning("Refresh token request rejected because the token is invalid or inactive.");
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = await _users.FindByIdAsync(persistedRefreshToken.UserId, cancellationToken);

        if (user is null || !user.Active)
        {
            _logger.LogWarning("Refresh token request rejected because the user is invalid or inactive.");
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var roles = await _roles.FindByUserIdAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(role => role.Name).ToArray();
        var permissions = await _roles.FindPermissionKeysByUserIdAsync(user.Id, cancellationToken);
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user, roleNames, permissions, cancellationToken);
        var newRefreshToken = _refreshTokenGenerator.Generate();
        var newRefreshTokenHash = _refreshTokenHasher.Hash(newRefreshToken);
        var now = DateTime.UtcNow;

        await _unitOfWork.ExecuteAsync(async () =>
        {
            persistedRefreshToken.Revoke(newRefreshTokenHash, now);

            await _refreshTokens.UpdateAsync(persistedRefreshToken, cancellationToken);
            await _refreshTokens.AddAsync(
                RefreshToken.Create(
                    user.Id,
                    newRefreshTokenHash,
                    now.AddDays(_jwtOptions.RefreshTokenExpirationDays),
                    now),
                cancellationToken);
        }, cancellationToken);

        _logger.LogInformation("Refresh token rotated successfully for user {UserId}.", user.Id);

        return new AuthOutput
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email.Value,
            Roles = roleNames,
            Token = new AuthToken
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = now.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes)
            }
        };
    }
}
