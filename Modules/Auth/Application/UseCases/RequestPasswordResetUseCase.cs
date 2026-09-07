using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Application.Services;
using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Auth.Application.UseCases;

public class RequestPasswordResetUseCase
{
    private const int PasswordResetTokenExpirationHours = 1;

    private readonly IUserRepository _users;
    private readonly ICaptchaVerificationService _captchaVerificationService;
    private readonly IPasswordResetTokenRepository _passwordResetTokens;
    private readonly IPasswordResetTokenHasher _passwordResetTokenHasher;
    private readonly IPasswordResetTokenGenerator _passwordResetTokenGenerator;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;
    private readonly FrontendOptions _frontendOptions;

    public RequestPasswordResetUseCase(
        IUserRepository users,
        ICaptchaVerificationService captchaVerificationService,
        IPasswordResetTokenRepository passwordResetTokens,
        IPasswordResetTokenHasher passwordResetTokenHasher,
        IPasswordResetTokenGenerator passwordResetTokenGenerator,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs,
        IOptions<FrontendOptions> frontendOptions)
    {
        _users = users;
        _captchaVerificationService = captchaVerificationService;
        _passwordResetTokens = passwordResetTokens;
        _passwordResetTokenHasher = passwordResetTokenHasher;
        _passwordResetTokenGenerator = passwordResetTokenGenerator;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
        _frontendOptions = frontendOptions.Value;
    }

    public async Task ExecuteAsync(
        RequestPasswordResetInput input,
        CancellationToken cancellationToken = default)
    {
        var captchaIsValid = await _captchaVerificationService.VerifyAsync(input.CaptchaToken, cancellationToken);

        if (!captchaIsValid)
        {
            throw new ApplicationValidationException("Captcha is invalid.");
        }

        if (string.IsNullOrWhiteSpace(input.Email))
        {
            throw new ApplicationValidationException("Email is invalid.");
        }

        var email = Email.Create(input.Email);
        var user = await _users.FindByEmailAsync(email, cancellationToken);

        if (user is null || !user.Active)
        {
            // Deliberately no error, no email — revealing whether an
            // account exists is an enumeration risk.
            return;
        }

        string resetTokenValue = string.Empty;

        await _unitOfWork.ExecuteAsync(async () =>
        {
            var now = DateTime.UtcNow;
            await _passwordResetTokens.InvalidateActiveByUserIdAsync(user.Id, now, cancellationToken);

            resetTokenValue = _passwordResetTokenGenerator.Generate();
            var resetTokenHash = _passwordResetTokenHasher.Hash(resetTokenValue);
            await _passwordResetTokens.AddAsync(
                PasswordResetToken.Create(
                    user.Id,
                    resetTokenHash,
                    now.AddHours(PasswordResetTokenExpirationHours),
                    now),
                cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.PasswordResetRequested,
                "User",
                user.Id,
                cancellationToken: cancellationToken);
        }, cancellationToken);

        await _emailSender.SendAsync(
            user.Email.Value,
            "Redefinição de senha",
            AuthEmailTemplates.BuildPasswordResetHtml(_frontendOptions.BaseUrl, resetTokenValue),
            cancellationToken);
    }
}
