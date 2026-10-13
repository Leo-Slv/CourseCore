using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.Validation;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Users.Application.Services;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class UpdateRoleUseCase
{
    private readonly IRoleRepository _roles;
    private readonly UserSessionInvalidator _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public UpdateRoleUseCase(
        IRoleRepository roles,
        UserSessionInvalidator sessions,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _roles = roles;
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<RoleDetailOutput> ExecuteAsync(
        UpdateRoleInput input,
        CancellationToken cancellationToken = default)
    {
        var name = (input.Name ?? string.Empty).Trim();
        var description = (input.Description ?? string.Empty).Trim();

        if (name.Length == 0 || name.Length > RoleValidationLimits.NameMaxLength)
        {
            throw new ApplicationValidationException("Name is invalid.");
        }

        if (description.Length > RoleValidationLimits.DescriptionMaxLength)
        {
            throw new ApplicationValidationException("Description is invalid.");
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var role = await _roles.FindByIdAsync(input.RoleId, cancellationToken)
                ?? throw new NotFoundException("Role not found.");

            if (RoleRules.IsAdmin(role))
            {
                throw new ForbiddenException("The Admin role cannot be modified.");
            }

            var existing = await _roles.FindByNameAsync(name, cancellationToken);

            if (existing is not null && existing.Id != role.Id)
            {
                throw new ConflictException("A role with this name already exists.");
            }

            var activeChanged = role.Active != input.Active;

            role.ChangeName(name);
            role.ChangeDescription(description);

            if (activeChanged && input.Active)
            {
                role.Activate();
            }
            else if (activeChanged)
            {
                role.Deactivate();
            }

            await _roles.UpdateAsync(role, cancellationToken);

            if (activeChanged)
            {
                var memberIds = await _roles.FindUserIdsByRoleIdAsync(role.Id, cancellationToken);
                await _sessions.InvalidateAsync(memberIds, cancellationToken);
            }

            await _auditLogs.RecordAsync(
                AuditLogActionNames.RoleUpdated,
                "Role",
                role.Id,
                new Dictionary<string, string?>
                {
                    ["displayName"] = role.Name,
                    ["active"] = role.Active.ToString()
                },
                cancellationToken: cancellationToken);

            var permissions = await _roles.FindPermissionKeysByRoleIdsAsync(cancellationToken);
            var counts = await _roles.CountMembersByRoleIdAsync(cancellationToken);

            return RoleDetailOutput.FromRole(
                role,
                isSystem: false,
                counts.GetValueOrDefault(role.Id),
                permissions.TryGetValue(role.Id, out var keys) ? keys : []);
        }, cancellationToken);
    }
}
