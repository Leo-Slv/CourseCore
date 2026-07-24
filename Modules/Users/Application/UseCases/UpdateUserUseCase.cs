using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Users.Application.DTOs;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Users.Application.UseCases;

public class UpdateUserUseCase
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public UpdateUserUseCase(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
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

            user.ChangeName(input.Name);

            if (user.Email != email)
            {
                user.ChangeEmail(email);
            }

            if (input.Active)
            {
                user.Activate();
            }
            else
            {
                user.Deactivate();
            }

            await _users.UpdateAsync(user, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.UserUpdated,
                "User",
                user.Id,
                cancellationToken: cancellationToken);

            return UserOutput.FromUser(user);
        }, cancellationToken);
    }
}
