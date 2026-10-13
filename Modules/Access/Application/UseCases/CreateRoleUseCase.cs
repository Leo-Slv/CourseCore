using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.Validation;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class CreateRoleUseCase
{
    private readonly IRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public CreateRoleUseCase(
        IRoleRepository roles,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _roles = roles;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<RoleDetailOutput> ExecuteAsync(
        CreateRoleInput input,
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
            if (await _roles.FindByNameAsync(name, cancellationToken) is not null)
            {
                throw new ConflictException("A role with this name already exists.");
            }

            var role = Role.Create(name, description);

            await _roles.CreateAsync(role, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.RoleCreated,
                "Role",
                role.Id,
                new Dictionary<string, string?> { ["displayName"] = role.Name },
                cancellationToken: cancellationToken);

            return RoleDetailOutput.FromRole(role, isSystem: false, memberCount: 0, permissionKeys: []);
        }, cancellationToken);
    }
}
