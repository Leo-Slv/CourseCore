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

public class ChangeOwnPasswordUseCase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordPolicy _passwordPolicy;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public ChangeOwnPasswordUseCase(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IPasswordPolicy passwordPolicy,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _passwordPolicy = passwordPolicy;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task ExecuteAsync(ChangeOwnPasswordInput input, CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(input));
        }

        _passwordPolicy.Validate(input.NewPassword);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var user = await _users.FindByIdAsync(input.UserId, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            if (!_passwordHasher.Verify(input.CurrentPassword, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            var passwordHash = _passwordHasher.Hash(input.NewPassword);
            user.ChangePasswordHash(passwordHash);
            user.IncrementTokenVersion();

            var revokedAt = DateTime.UtcNow;
            var revokedRefreshTokens = await _refreshTokens.RevokeActiveByUserIdAsync(
                user.Id,
                revokedAt,
                cancellationToken);

            await _users.UpdateAsync(user, cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.PasswordChanged,
                "User",
                user.Id,
                userId: user.Id,
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
