using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Users.Application.UseCases;

public class RemoveUserRoleUseCase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public RemoveUserRoleUseCase(
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

    public Task ExecuteAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("RoleId is required.", nameof(roleId));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var user = await _users.FindByIdAsync(userId, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            var role = await _roles.FindByIdAsync(roleId, cancellationToken);

            if (role is null)
            {
                throw new NotFoundException("Role not found.");
            }

            await _roles.RemoveFromUserAsync(userId, roleId, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.UserRoleUnassigned,
                "User",
                userId,
                new Dictionary<string, string?>
                {
                    ["roleId"] = roleId.ToString(),
                    ["displayName"] = $"{user.Email.Value} → {role.Name}"
                },
                cancellationToken: cancellationToken);
        }, cancellationToken);
    }
}
