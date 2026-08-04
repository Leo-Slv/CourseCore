using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Users.Application.DTOs;
using CourseCore.Api.Modules.Users.Application.Validation;
using CourseCore.Api.Modules.Users.Domain.Entities;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Users.Application.UseCases;

public class CreateUserUseCase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordPolicy _passwordPolicy;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public CreateUserUseCase(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IPasswordPolicy passwordPolicy,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _passwordPolicy = passwordPolicy;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<UserOutput> ExecuteAsync(
        CreateUserInput input,
        CancellationToken cancellationToken = default)
    {
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

        return _unitOfWork.ExecuteAsync(async () =>
        {
            if (await _users.ExistsByEmailAsync(email, cancellationToken))
            {
                throw new ConflictException("A user with this email already exists.");
            }

            var passwordHash = _passwordHasher.Hash(input.Password);
            var user = User.Create(input.Name, email, passwordHash);

            await _users.CreateAsync(user, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.UserCreated,
                "User",
                user.Id,
                cancellationToken: cancellationToken);

            return UserOutput.FromUser(user);
        }, cancellationToken);
    }
}
