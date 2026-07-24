using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class GrantUserAreaAccessUseCase
{
    private readonly IUserRepository _users;
    private readonly IAreaRepository _areas;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public GrantUserAreaAccessUseCase(
        IUserRepository users,
        IAreaRepository areas,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
        _areas = areas;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<AreaAccessOutput> ExecuteAsync(
        GrantUserAreaAccessInput input,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(input.UserId, input.AreaId);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var user = await _users.FindByIdAsync(input.UserId, cancellationToken);
            var area = await _areas.FindByIdAsync(input.AreaId, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            if (area is null)
            {
                throw new NotFoundException("Area not found.");
            }

            var access = UserAreaAccess.Create(
                input.UserId,
                input.AreaId,
                input.CanView,
                input.CanManage,
                input.StartsAt,
                input.ExpiresAt);

            await _areas.CreateUserAreaAccessAsync(access, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.UserAreaAccessGranted,
                "UserAreaAccess",
                access.Id,
                new Dictionary<string, string?>
                {
                    ["targetUserId"] = access.UserId.ToString(),
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

    private static void ValidateIds(Guid userId, Guid areaId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.");
        }

        if (areaId == Guid.Empty)
        {
            throw new ArgumentException("AreaId is required.");
        }
    }
}
