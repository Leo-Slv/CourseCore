using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
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

    public RefreshTokenUseCase(
        IUserRepository users,
        IRoleRepository roles,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        IRefreshTokenHasher refreshTokenHasher,
        IRefreshTokenGenerator refreshTokenGenerator,
        IUnitOfWork unitOfWork,
        IOptions<JwtOptions> jwtOptions)
    {
        _users = users;
        _roles = roles;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _refreshTokenHasher = refreshTokenHasher;
        _refreshTokenGenerator = refreshTokenGenerator;
        _unitOfWork = unitOfWork;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthOutput> ExecuteAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var refreshTokenHash = _refreshTokenHasher.Hash(refreshToken);
        var persistedRefreshToken = await _refreshTokens.FindByTokenHashAsync(
            refreshTokenHash,
            cancellationToken);

        if (persistedRefreshToken is null || !persistedRefreshToken.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = await _users.FindByIdAsync(persistedRefreshToken.UserId, cancellationToken);

        if (user is null || !user.Active)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var roles = await _roles.FindByUserIdAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(role => role.Name).ToArray();
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user, roleNames, cancellationToken);
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
