using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class GrantRoleAreaAccessUseCase
{
    private readonly IRoleRepository _roles;
    private readonly IAreaRepository _areas;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public GrantRoleAreaAccessUseCase(
        IRoleRepository roles,
        IAreaRepository areas,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _roles = roles;
        _areas = areas;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<AreaAccessOutput> ExecuteAsync(
        GrantRoleAreaAccessInput input,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(input.RoleId, input.AreaId);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var role = await _roles.FindByIdAsync(input.RoleId, cancellationToken);
            var area = await _areas.FindByIdAsync(input.AreaId, cancellationToken);

            if (role is null)
            {
                throw new NotFoundException("Role not found.");
            }

            if (area is null)
            {
                throw new NotFoundException("Area not found.");
            }

            var access = RoleAreaAccess.Create(
                input.RoleId,
                input.AreaId,
                input.CanView,
                input.CanManage);

            await _areas.CreateRoleAreaAccessAsync(access, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.RoleAreaAccessGranted,
                "RoleAreaAccess",
                access.Id,
                new Dictionary<string, string?>
                {
                    ["roleId"] = access.RoleId.ToString(),
                    ["areaId"] = access.AreaId.ToString(),
                    ["canView"] = access.CanView.ToString(),
                    ["canManage"] = access.CanManage.ToString()
                },
                cancellationToken: cancellationToken);

            return new AreaAccessOutput
            {
                AreaId = access.AreaId,
                CanView = access.CanView,
                CanManage = access.CanManage
            };
        }, cancellationToken);
    }

    private static void ValidateIds(Guid roleId, Guid areaId)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("RoleId is required.");
        }

        if (areaId == Guid.Empty)
        {
            throw new ArgumentException("AreaId is required.");
        }
    }
}
