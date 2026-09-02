using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Auth.Application.UseCases;

public class ResendEmailConfirmationUseCase
{
    private const int EmailVerificationTokenExpirationHours = 24;

    private readonly IUserRepository _users;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokens;
    private readonly IEmailVerificationTokenHasher _emailVerificationTokenHasher;
    private readonly IEmailVerificationTokenGenerator _emailVerificationTokenGenerator;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public ResendEmailConfirmationUseCase(
        IUserRepository users,
        IEmailVerificationTokenRepository emailVerificationTokens,
        IEmailVerificationTokenHasher emailVerificationTokenHasher,
        IEmailVerificationTokenGenerator emailVerificationTokenGenerator,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
        _emailVerificationTokens = emailVerificationTokens;
        _emailVerificationTokenHasher = emailVerificationTokenHasher;
        _emailVerificationTokenGenerator = emailVerificationTokenGenerator;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public async Task ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var user = await _users.FindByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        if (user.EmailVerifiedAt.HasValue)
        {
            throw new ConflictException("Email is already confirmed.");
        }

        string verificationTokenValue = string.Empty;

        await _unitOfWork.ExecuteAsync(async () =>
        {
            var now = DateTime.UtcNow;
            await _emailVerificationTokens.InvalidateActiveByUserIdAsync(user.Id, now, cancellationToken);

            verificationTokenValue = _emailVerificationTokenGenerator.Generate();
            var verificationTokenHash = _emailVerificationTokenHasher.Hash(verificationTokenValue);
            await _emailVerificationTokens.AddAsync(
                EmailVerificationToken.Create(
                    user.Id,
                    verificationTokenHash,
                    now.AddHours(EmailVerificationTokenExpirationHours),
                    now),
                cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.EmailVerificationResent,
                "User",
                user.Id,
                cancellationToken: cancellationToken);
        }, cancellationToken);

        await _emailSender.SendAsync(
            user.Email.Value,
            "Confirme seu e-mail",
            BuildVerificationEmailHtml(verificationTokenValue),
            cancellationToken);
    }

    private static string BuildVerificationEmailHtml(string token)
    {
        return $"<p>Use o código a seguir para confirmar seu e-mail:</p><p><strong>{token}</strong></p>";
    }
}
