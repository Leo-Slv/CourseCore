using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class ApproveAccessRequestUseCase
{
    private readonly IAccessRequestRepository _accessRequests;
    private readonly ICourseRepository _courses;
    private readonly IAreaRepository _areas;
    private readonly IUserRepository _users;
    private readonly UserCourseAccessGranter _courseAccessGranter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public ApproveAccessRequestUseCase(
        IAccessRequestRepository accessRequests,
        ICourseRepository courses,
        IAreaRepository areas,
        IUserRepository users,
        UserCourseAccessGranter courseAccessGranter,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _accessRequests = accessRequests;
        _courses = courses;
        _areas = areas;
        _users = users;
        _courseAccessGranter = courseAccessGranter;
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

            await _courseAccessGranter.EnsureGrantedAsync(
                request.UserId,
                request.CourseId,
                CourseAccessSource.Manual,
                cancellationToken);

            request.Approve(input.DecidedByUserId);
            await _accessRequests.UpdateAsync(request, cancellationToken);

            var requestingUser = await _users.FindByIdAsync(request.UserId, cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.AccessRequestApproved,
                "AccessRequest",
                request.Id,
                new Dictionary<string, string?>
                {
                    ["targetUserId"] = request.UserId.ToString(),
                    ["courseId"] = request.CourseId.ToString(),
                    ["displayName"] = $"{requestingUser?.Email.Value ?? request.UserId.ToString()} → {course.Title}"
                },
                userId: input.DecidedByUserId,
                cancellationToken: cancellationToken);

            return AccessRequestOutput.FromAccessRequest(request);
        }, cancellationToken);
    }
}
