using System.Globalization;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Auth.Application.UseCases;

public class ConfirmPasswordResetUseCase
{
    private readonly IUserRepository _users;
    private readonly IPasswordResetTokenRepository _passwordResetTokens;
    private readonly IPasswordResetTokenHasher _passwordResetTokenHasher;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordPolicy _passwordPolicy;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public ConfirmPasswordResetUseCase(
        IUserRepository users,
        IPasswordResetTokenRepository passwordResetTokens,
        IPasswordResetTokenHasher passwordResetTokenHasher,
        IPasswordHasher passwordHasher,
        IPasswordPolicy passwordPolicy,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
        _passwordResetTokens = passwordResetTokens;
        _passwordResetTokenHasher = passwordResetTokenHasher;
        _passwordHasher = passwordHasher;
        _passwordPolicy = passwordPolicy;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public async Task ExecuteAsync(
        ConfirmPasswordResetInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Token))
        {
            throw new ApplicationValidationException("Reset token is invalid, expired or already used.");
        }

        _passwordPolicy.Validate(input.NewPassword);

        var tokenHash = _passwordResetTokenHasher.Hash(input.Token);
        var token = await _passwordResetTokens.FindByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null || !token.IsActive)
        {
            throw new ApplicationValidationException("Reset token is invalid, expired or already used.");
        }

        await _unitOfWork.ExecuteAsync(async () =>
        {
            var consumedAt = DateTime.UtcNow;
            var consumed = await _passwordResetTokens.TryConsumeAsync(
                token.Id,
                token.TokenHash,
                consumedAt,
                cancellationToken);

            if (!consumed)
            {
                throw new ApplicationValidationException("Reset token is invalid, expired or already used.");
            }

            var user = await _users.FindByIdAsync(token.UserId, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            var passwordHash = _passwordHasher.Hash(input.NewPassword);
            user.ChangePasswordHash(passwordHash);
            user.IncrementTokenVersion();

            var revokedRefreshTokens = await _refreshTokens.RevokeActiveByUserIdAsync(
                user.Id,
                consumedAt,
                cancellationToken);

            await _users.UpdateAsync(user, cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.PasswordResetSucceeded,
                "User",
                user.Id,
                cancellationToken: cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.UserTokenVersionIncremented,
                "User",
                user.Id,
                new Dictionary<string, string?>
                {
                    ["tokenVersion"] = user.TokenVersion.ToString(CultureInfo.InvariantCulture)
                },
                user.Id,
                cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.UserSessionsRevoked,
                "User",
                user.Id,
                new Dictionary<string, string?>
                {
                    ["revokedRefreshTokens"] = revokedRefreshTokens.ToString(CultureInfo.InvariantCulture)
                },
                user.Id,
                cancellationToken);
        }, cancellationToken);
    }
}
