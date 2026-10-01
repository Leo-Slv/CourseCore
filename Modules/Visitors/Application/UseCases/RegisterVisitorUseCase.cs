using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Visitors.Application.DTOs;
using CourseCore.Api.Modules.Visitors.Application.Validation;
using CourseCore.Api.Modules.Visitors.Domain.Entities;
using CourseCore.Api.Modules.Visitors.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Visitors.Application.UseCases;

public class RegisterVisitorUseCase
{
    private readonly ICaptchaVerificationService _captchaVerificationService;
    private readonly IVisitorRepository _visitors;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public RegisterVisitorUseCase(
        ICaptchaVerificationService captchaVerificationService,
        IVisitorRepository visitors,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _captchaVerificationService = captchaVerificationService;
        _visitors = visitors;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public async Task<RegisterVisitorOutput> ExecuteAsync(
        RegisterVisitorInput input,
        CancellationToken cancellationToken = default)
    {
        var captchaIsValid = await _captchaVerificationService.VerifyAsync(input.CaptchaToken, cancellationToken);

        if (!captchaIsValid)
        {
            throw new ApplicationValidationException("Captcha is invalid.");
        }

        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > VisitorValidationLimits.NameMaxLength)
        {
            throw new ApplicationValidationException("Name is invalid.");
        }

        if (string.IsNullOrWhiteSpace(input.Phone) || input.Phone.Trim().Length > VisitorValidationLimits.PhoneMaxLength)
        {
            throw new ApplicationValidationException("Phone is invalid.");
        }

        if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Trim().Length > VisitorValidationLimits.EmailMaxLength)
        {
            throw new ApplicationValidationException("Email is invalid.");
        }

        if (input.Address is not null && input.Address.Trim().Length > VisitorValidationLimits.AddressMaxLength)
        {
            throw new ApplicationValidationException("Address is invalid.");
        }

        var visitor = Visitor.Create(input.Name, input.Phone, Email.Create(input.Email), input.Address);

        return await _unitOfWork.ExecuteAsync(async () =>
        {
            await _visitors.CreateAsync(visitor, cancellationToken);

            // Visitors have no account; keep their personal data out of the audit trail.
            await _auditLogs.RecordAsync(
                AuditLogActionNames.VisitorRegistered,
                "Visitor",
                visitor.Id,
                cancellationToken: cancellationToken);

            return new RegisterVisitorOutput
            {
                Id = visitor.Id,
                SubmittedAt = visitor.CreatedAt
            };
        }, cancellationToken);
    }
}
