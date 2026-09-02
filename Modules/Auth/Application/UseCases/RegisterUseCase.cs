using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Application.Services;
using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Users.Application.Validation;
using CourseCore.Api.Modules.Users.Domain.Entities;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Auth.Application.UseCases;

public class RegisterUseCase
{
    private const int EmailVerificationTokenExpirationHours = 24;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordPolicy _passwordPolicy;
    private readonly ICaptchaVerificationService _captchaVerificationService;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokens;
    private readonly IEmailVerificationTokenHasher _emailVerificationTokenHasher;
    private readonly IEmailVerificationTokenGenerator _emailVerificationTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly SessionIssuer _sessionIssuer;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public RegisterUseCase(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IPasswordPolicy passwordPolicy,
        ICaptchaVerificationService captchaVerificationService,
        IEmailVerificationTokenRepository emailVerificationTokens,
        IEmailVerificationTokenHasher emailVerificationTokenHasher,
        IEmailVerificationTokenGenerator emailVerificationTokenGenerator,
        IRefreshTokenRepository refreshTokens,
        SessionIssuer sessionIssuer,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _passwordPolicy = passwordPolicy;
        _captchaVerificationService = captchaVerificationService;
        _emailVerificationTokens = emailVerificationTokens;
        _emailVerificationTokenHasher = emailVerificationTokenHasher;
        _emailVerificationTokenGenerator = emailVerificationTokenGenerator;
        _refreshTokens = refreshTokens;
        _sessionIssuer = sessionIssuer;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public async Task<AuthOutput> ExecuteAsync(
        RegisterInput input,
        CancellationToken cancellationToken = default)
    {
        var captchaIsValid = await _captchaVerificationService.VerifyAsync(input.CaptchaToken, cancellationToken);

        if (!captchaIsValid)
        {
            throw new ApplicationValidationException("Captcha is invalid.");
        }

        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > UserValidationLimits.NameMaxLength)
        {
            throw new ApplicationValidationException("Name is invalid.");
        }

        if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Trim().Length > UserValidationLimits.EmailMaxLength)
        {
            throw new ApplicationValidationException("Email is invalid.");
        }

        _passwordPolicy.Validate(input.Password);

        var email = Email.Create(input.Email);
        string verificationTokenValue = string.Empty;
        AuthOutput? authOutput = null;

        await _unitOfWork.ExecuteAsync(async () =>
        {
            if (await _users.ExistsByEmailAsync(email, cancellationToken))
            {
                throw new ConflictException("A user with this email already exists.");
            }

            var passwordHash = _passwordHasher.Hash(input.Password);
            var user = User.Create(input.Name, email, passwordHash);
            await _users.CreateAsync(user, cancellationToken);

            var now = DateTime.UtcNow;
            verificationTokenValue = _emailVerificationTokenGenerator.Generate();
            var verificationTokenHash = _emailVerificationTokenHasher.Hash(verificationTokenValue);
            await _emailVerificationTokens.AddAsync(
                EmailVerificationToken.Create(
                    user.Id,
                    verificationTokenHash,
                    now.AddHours(EmailVerificationTokenExpirationHours),
                    now),
                cancellationToken);

            var session = await _sessionIssuer.BuildAsync(user, cancellationToken);
            await _refreshTokens.AddAsync(session.RefreshToken, cancellationToken);
            authOutput = session.Output;

            await _auditLogs.RecordAsync(
                AuditLogActionNames.UserRegistered,
                "User",
                user.Id,
                cancellationToken: cancellationToken);
        }, cancellationToken);

        await _emailSender.SendAsync(
            email.Value,
            "Confirme seu e-mail",
            BuildVerificationEmailHtml(verificationTokenValue),
            cancellationToken);

        return authOutput!;
    }

    private static string BuildVerificationEmailHtml(string token)
    {
        return $"<p>Use o código a seguir para confirmar seu e-mail:</p><p><strong>{token}</strong></p>";
    }
}
