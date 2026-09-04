using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class ApproveAccessRequestUseCase
{
    private readonly IAccessRequestRepository _accessRequests;
    private readonly ICourseRepository _courses;
    private readonly IAreaRepository _areas;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public ApproveAccessRequestUseCase(
        IAccessRequestRepository accessRequests,
        ICourseRepository courses,
        IAreaRepository areas,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _accessRequests = accessRequests;
        _courses = courses;
        _areas = areas;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<AccessRequestOutput> ExecuteAsync(
        ApproveAccessRequestInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.AccessRequestId == Guid.Empty)
        {
            throw new ArgumentException("AccessRequestId is required.", nameof(input));
        }

        if (input.DecidedByUserId == Guid.Empty)
        {
            throw new ArgumentException("DecidedByUserId is required.", nameof(input));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var request = await _accessRequests.FindByIdAsync(input.AccessRequestId, cancellationToken);

            if (request is null)
            {
                throw new NotFoundException("Access request not found.");
            }

            if (request.Status != AccessRequestStatus.Pending)
            {
                throw new ConflictException("Access request has already been decided.");
            }

            var course = await _courses.FindByIdAsync(request.CourseId, cancellationToken);

            if (course is null)
            {
                throw new NotFoundException("Course not found.");
            }

            var areas = await _areas.ListAsync(cancellationToken);
            var targetAreaIds = course.AreaIds
                .Where(areaId => areas.Any(area => area.Id == areaId && area.Active))
                .ToList();

            if (targetAreaIds.Count == 0)
            {
                throw new ConflictException("Course has no active linked areas to grant.");
            }

            foreach (var areaId in targetAreaIds)
            {
                var existing = await _areas.FindUserAreaAccessAsync(request.UserId, areaId, cancellationToken);

                if (existing is null)
                {
                    var access = UserAreaAccess.Create(request.UserId, areaId, canView: true, canManage: false);
                    await _areas.CreateUserAreaAccessAsync(access, cancellationToken);
                }
                else
                {
                    existing.ChangePermissions(canView: true, existing.CanManage);
                    await _areas.UpdateUserAreaAccessAsync(existing, cancellationToken);
                }
            }

            request.Approve(input.DecidedByUserId);
            await _accessRequests.UpdateAsync(request, cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.AccessRequestApproved,
                "AccessRequest",
                request.Id,
                new Dictionary<string, string?>
                {
                    ["targetUserId"] = request.UserId.ToString(),
                    ["courseId"] = request.CourseId.ToString(),
                    ["grantedAreaIds"] = string.Join(",", targetAreaIds)
                },
                userId: input.DecidedByUserId,
                cancellationToken: cancellationToken);

            return AccessRequestOutput.FromAccessRequest(request);
        }, cancellationToken);
    }
}
