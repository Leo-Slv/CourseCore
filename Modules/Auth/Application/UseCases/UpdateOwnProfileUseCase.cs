using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Users.Application.Validation;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Auth.Application.UseCases;

public class UpdateOwnProfileUseCase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public UpdateOwnProfileUseCase(
        IUserRepository users,
        IRoleRepository roles,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
        _roles = roles;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<CurrentUserOutput> ExecuteAsync(
        UpdateOwnProfileInput input,
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

        if (input.Phone is not null && input.Phone.Trim().Length > UserValidationLimits.PhoneMaxLength)
        {
            throw new ApplicationValidationException("Phone is invalid.");
        }

        if (input.AvatarUrl is not null && input.AvatarUrl.Trim().Length > UserValidationLimits.AvatarUrlMaxLength)
        {
            throw new ApplicationValidationException("AvatarUrl is invalid.");
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var user = await _users.FindByIdAsync(input.UserId, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            var requestedName = input.Name.Trim();
            var nameChanged = !string.Equals(user.Name, requestedName, StringComparison.Ordinal);
            var phoneChanged = user.Phone != NormalizeOrNull(input.Phone);
            var avatarUrlChanged = user.AvatarUrl != NormalizeOrNull(input.AvatarUrl);

            if (nameChanged)
            {
                user.ChangeName(requestedName);
            }

            if (phoneChanged)
            {
                user.ChangePhone(input.Phone);
            }

            if (avatarUrlChanged)
            {
                user.ChangeAvatarUrl(input.AvatarUrl);
            }

            if (nameChanged || phoneChanged || avatarUrlChanged)
            {
                await _users.UpdateAsync(user, cancellationToken);

                await _auditLogs.RecordAsync(
                    AuditLogActionNames.UserProfileUpdated,
                    "User",
                    user.Id,
                    userId: user.Id,
                    cancellationToken: cancellationToken);
            }

            var roles = await _roles.FindByUserIdAsync(user.Id, cancellationToken);

            return new CurrentUserOutput
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email.Value,
                Active = user.Active,
                EmailVerifiedAt = user.EmailVerifiedAt,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                Roles = roles.Select(role => role.Name).ToList()
            };
        }, cancellationToken);
    }

    private static string? NormalizeOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
