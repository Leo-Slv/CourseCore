using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Users.Application.Services;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class SetRolePermissionsUseCase
{
    private readonly IRoleRepository _roles;
    private readonly IPermissionRepository _permissions;
    private readonly UserSessionInvalidator _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public SetRolePermissionsUseCase(
        IRoleRepository roles,
        IPermissionRepository permissions,
        UserSessionInvalidator sessions,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _roles = roles;
        _permissions = permissions;
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<RoleDetailOutput> ExecuteAsync(
        SetRolePermissionsInput input,
        CancellationToken cancellationToken = default)
    {
        var requested = (input.PermissionKeys ?? [])
            .Select(key => key?.Trim() ?? string.Empty)
            .Where(key => key.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var role = await _roles.FindByIdAsync(input.RoleId, cancellationToken)
                ?? throw new NotFoundException("Role not found.");

            if (RoleRules.IsAdmin(role))
            {
                throw new ForbiddenException("The Admin role cannot be modified.");
            }

            var catalog = (await _permissions.ListAsync(cancellationToken))
                .Select(permission => permission.Key)
                .ToHashSet(StringComparer.Ordinal);
            var unknown = requested.Where(key => !catalog.Contains(key)).ToList();

            if (unknown.Count > 0)
            {
                throw new ApplicationValidationException(
                    "PermissionKeys is invalid.",
                    unknown.Select(key => $"Unknown permission: {key}").ToList());
            }

            var before = await _roles.FindPermissionKeysByRoleIdsAsync(cancellationToken);
            IReadOnlyCollection<string> previous = before.TryGetValue(role.Id, out var current) ? current : [];

            await _roles.ReplacePermissionsAsync(role.Id, requested, cancellationToken);

            var memberIds = await _roles.FindUserIdsByRoleIdAsync(role.Id, cancellationToken);
            await _sessions.InvalidateAsync(memberIds, cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.RolePermissionsChanged,
                "Role",
                role.Id,
                new Dictionary<string, string?>
                {
                    ["displayName"] = role.Name,
                    ["granted"] = string.Join(", ", requested.Except(previous).OrderBy(key => key)),
                    ["removed"] = string.Join(", ", previous.Except(requested).OrderBy(key => key))
                },
                cancellationToken: cancellationToken);

            var counts = await _roles.CountMembersByRoleIdAsync(cancellationToken);

            return RoleDetailOutput.FromRole(
                role,
                isSystem: false,
                counts.GetValueOrDefault(role.Id),
                requested.OrderBy(key => key).ToList());
        }, cancellationToken);
    }
}
