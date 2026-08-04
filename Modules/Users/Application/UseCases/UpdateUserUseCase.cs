using System.Globalization;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Users.Application.DTOs;
using CourseCore.Api.Modules.Users.Application.Validation;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Users.Application.UseCases;

public class UpdateUserUseCase
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public UpdateUserUseCase(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<UserOutput> ExecuteAsync(
        UpdateUserInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > UserValidationLimits.NameMaxLength)
        {
            throw new ApplicationValidationException("Name is invalid.");
        }

        if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Trim().Length > UserValidationLimits.EmailMaxLength)
        {
            throw new ApplicationValidationException("Email is invalid.");
        }

        var email = Email.Create(input.Email);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var user = await _users.FindByIdAsync(input.UserId, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            if (user.Email != email && await _users.ExistsByEmailAsync(email, cancellationToken))
            {
                throw new ConflictException("A user with this email already exists.");
            }

            var requestedName = input.Name?.Trim() ?? string.Empty;
            var nameChanged = !string.Equals(user.Name, requestedName, StringComparison.Ordinal);
            var emailChanged = user.Email != email;
            var activeChanged = user.Active != input.Active;

            if (nameChanged)
            {
                user.ChangeName(requestedName);
            }

            if (emailChanged)
            {
                user.ChangeEmail(email);
            }

            if (activeChanged && input.Active)
            {
                user.Activate();
            }
            else if (activeChanged)
            {
                user.Deactivate();
            }

            var shouldInvalidateSessions = nameChanged || emailChanged || activeChanged;
            var revokedRefreshTokens = 0;

            if (shouldInvalidateSessions)
            {
                user.IncrementTokenVersion();
                revokedRefreshTokens = await _refreshTokens.RevokeActiveByUserIdAsync(
                    user.Id,
                    DateTime.UtcNow,
                    cancellationToken);
            }

            await _users.UpdateAsync(user, cancellationToken);
            if (shouldInvalidateSessions)
            {
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
            }

            await _auditLogs.RecordAsync(
                AuditLogActionNames.UserUpdated,
                "User",
                user.Id,
                cancellationToken: cancellationToken);

            return UserOutput.FromUser(user);
        }, cancellationToken);
    }
}
