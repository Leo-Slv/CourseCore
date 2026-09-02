using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Auth.Application.UseCases;

public class ConfirmEmailUseCase
{
    private readonly IUserRepository _users;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokens;
    private readonly IEmailVerificationTokenHasher _emailVerificationTokenHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public ConfirmEmailUseCase(
        IUserRepository users,
        IEmailVerificationTokenRepository emailVerificationTokens,
        IEmailVerificationTokenHasher emailVerificationTokenHasher,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
        _emailVerificationTokens = emailVerificationTokens;
        _emailVerificationTokenHasher = emailVerificationTokenHasher;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public async Task ExecuteAsync(
        ConfirmEmailInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty || string.IsNullOrWhiteSpace(input.Token))
        {
            throw new ApplicationValidationException("Confirmation token is invalid.");
        }

        var tokenHash = _emailVerificationTokenHasher.Hash(input.Token);
        var token = await _emailVerificationTokens.FindByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null || token.UserId != input.UserId || !token.IsActive)
        {
            throw new ApplicationValidationException("Confirmation token is invalid, expired or already used.");
        }

        await _unitOfWork.ExecuteAsync(async () =>
        {
            var consumedAt = DateTime.UtcNow;
            var consumed = await _emailVerificationTokens.TryConsumeAsync(
                token.Id,
                token.TokenHash,
                consumedAt,
                cancellationToken);

            if (!consumed)
            {
                throw new ApplicationValidationException("Confirmation token is invalid, expired or already used.");
            }

            var user = await _users.FindByIdAsync(input.UserId, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            user.MarkEmailAsVerified(consumedAt);
            await _users.UpdateAsync(user, cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.UserEmailVerified,
                "User",
                user.Id,
                cancellationToken: cancellationToken);
        }, cancellationToken);
    }
}
