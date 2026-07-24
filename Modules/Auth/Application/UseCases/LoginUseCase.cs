using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Modules.Users.Domain.Entities;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Auth.Application.UseCases;

public class LoginUseCase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<LoginUseCase> _logger;

    public LoginUseCase(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        IRefreshTokenHasher refreshTokenHasher,
        IRefreshTokenGenerator refreshTokenGenerator,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs,
        IOptions<JwtOptions> jwtOptions,
        ILogger<LoginUseCase> logger)
    {
        _users = users;
        _roles = roles;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _refreshTokenHasher = refreshTokenHasher;
        _refreshTokenGenerator = refreshTokenGenerator;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<AuthOutput> ExecuteAsync(
        LoginInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
        {
            _logger.LogWarning("Login attempt rejected because credentials were incomplete.");
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var email = Email.Create(input.Email);
        var user = await _users.FindByEmailAsync(email, cancellationToken);

        if (user is null || !user.Active || !_passwordHasher.Verify(input.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login attempt rejected with invalid credentials.");
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var roles = await _roles.FindByUserIdAsync(user.Id, cancellationToken);
        var roleNames = roles.Select(role => role.Name).ToArray();
        var permissions = await _roles.FindPermissionKeysByUserIdAsync(user.Id, cancellationToken);
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user, roleNames, permissions, cancellationToken);
        var refreshToken = _refreshTokenGenerator.Generate();
        var refreshTokenHash = _refreshTokenHasher.Hash(refreshToken);
        var now = DateTime.UtcNow;

        await _unitOfWork.ExecuteAsync(async () =>
        {
            await _refreshTokens.AddAsync(
                RefreshToken.Create(
                    user.Id,
                    refreshTokenHash,
                    now.AddDays(_jwtOptions.RefreshTokenExpirationDays),
                    now),
                cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.LoginSucceeded,
                "User",
                user.Id,
                new Dictionary<string, string?> { ["result"] = "succeeded" },
                user.Id,
                cancellationToken);
        }, cancellationToken);

        _logger.LogInformation("User {UserId} signed in successfully.", user.Id);

        return new AuthOutput
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email.Value,
            Roles = roleNames,
            Token = new AuthToken
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = now.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes)
            }
        };
    }
}
