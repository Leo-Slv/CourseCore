using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class GrantCourseAccessUseCase
{
    private readonly IUserRepository _users;
    private readonly ICourseRepository _courses;
    private readonly IAreaRepository _areas;
    private readonly IAccessRequestRepository _accessRequests;
    private readonly CourseAccessService _courseAccessService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public GrantCourseAccessUseCase(
        IUserRepository users,
        ICourseRepository courses,
        IAreaRepository areas,
        IAccessRequestRepository accessRequests,
        CourseAccessService courseAccessService,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
        _courses = courses;
        _areas = areas;
        _accessRequests = accessRequests;
        _courseAccessService = courseAccessService;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<AccessRequestOutput> ExecuteAsync(
        Guid userId,
        Guid courseId,
        Guid decidedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (courseId == Guid.Empty)
        {
            throw new ArgumentException("CourseId is required.", nameof(courseId));
        }

        if (decidedByUserId == Guid.Empty)
        {
            throw new ArgumentException("DecidedByUserId is required.", nameof(decidedByUserId));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var user = await _users.FindByIdAsync(userId, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            var course = await _courses.FindByIdAsync(courseId, cancellationToken);

            if (course is null || !course.Published)
            {
                throw new NotFoundException("Course not found.");
            }

            if (course.PricingModel == CoursePricingModel.Free)
            {
                throw new ConflictException("Course is free; no grant needed.");
            }

            var access = await _courseAccessService.CanUserAccessCourseAsync(userId, courseId, cancellationToken);

            if (access.CanAccess)
            {
                throw new ConflictException("User already has access to this course.");
            }

            var pendingRequest = await _accessRequests.FindPendingByUserAndCourseAsync(userId, courseId, cancellationToken);
            var isNewRequest = pendingRequest is null;
            var request = pendingRequest ?? AccessRequest.Create(userId, courseId);

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
                var existingAccess = await _areas.FindUserAreaAccessAsync(userId, areaId, cancellationToken);

                if (existingAccess is null)
                {
                    var newAccess = UserAreaAccess.Create(userId, areaId, canView: true, canManage: false);
                    await _areas.CreateUserAreaAccessAsync(newAccess, cancellationToken);
                }
                else
                {
                    existingAccess.ChangePermissions(canView: true, existingAccess.CanManage);
                    await _areas.UpdateUserAreaAccessAsync(existingAccess, cancellationToken);
                }
            }

            request.Approve(decidedByUserId);

            if (isNewRequest)
            {
                await _accessRequests.CreateAsync(request, cancellationToken);
            }
            else
            {
                await _accessRequests.UpdateAsync(request, cancellationToken);
            }

            await _auditLogs.RecordAsync(
                AuditLogActionNames.AccessRequestApproved,
                "AccessRequest",
                request.Id,
                new Dictionary<string, string?>
                {
                    ["targetUserId"] = userId.ToString(),
                    ["courseId"] = courseId.ToString(),
                    ["grantedAreaIds"] = string.Join(",", targetAreaIds),
                    ["grantedDirectly"] = "true"
                },
                userId: decidedByUserId,
                cancellationToken: cancellationToken);

            return AccessRequestOutput.FromAccessRequest(request);
        }, cancellationToken);
    }
}
