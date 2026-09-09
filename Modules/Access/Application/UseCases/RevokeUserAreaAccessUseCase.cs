using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class RevokeUserAreaAccessUseCase
{
    private readonly IAreaRepository _areas;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public RevokeUserAreaAccessUseCase(
        IAreaRepository areas,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _areas = areas;
        _users = users;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task ExecuteAsync(Guid userId, Guid areaId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (areaId == Guid.Empty)
        {
            throw new ArgumentException("AreaId is required.", nameof(areaId));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var access = await _areas.FindUserAreaAccessAsync(userId, areaId, cancellationToken);

            if (access is null)
            {
                throw new NotFoundException("User area access not found.");
            }

            access.Revoke();
            await _areas.UpdateUserAreaAccessAsync(access, cancellationToken);

            var user = await _users.FindByIdAsync(userId, cancellationToken);
            var area = await _areas.FindByIdAsync(areaId, cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.UserAreaAccessRevoked,
                "UserAreaAccess",
                access.Id,
                new Dictionary<string, string?>
                {
                    ["targetUserId"] = userId.ToString(),
                    ["areaId"] = areaId.ToString(),
                    ["displayName"] = $"{user?.Email.Value ?? userId.ToString()} → {area?.Name ?? areaId.ToString()}"
                },
                cancellationToken: cancellationToken);
        }, cancellationToken);
    }
}
