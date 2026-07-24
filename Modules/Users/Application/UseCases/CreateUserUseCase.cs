using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Users.Application.DTOs;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public CreateUserUseCase(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<UserOutput> ExecuteAsync(
        CreateUserInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new ArgumentException("Name is required.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            throw new ArgumentException("Password is required.", nameof(input));
        }

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
