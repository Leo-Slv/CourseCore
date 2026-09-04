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

public class RequestCourseAccessUseCase
{
    private readonly IUserRepository _users;
    private readonly ICourseRepository _courses;
    private readonly IAccessRequestRepository _accessRequests;
    private readonly CourseAccessService _courseAccessService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public RequestCourseAccessUseCase(
        IUserRepository users,
        ICourseRepository courses,
        IAccessRequestRepository accessRequests,
        CourseAccessService courseAccessService,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _users = users;
        _courses = courses;
        _accessRequests = accessRequests;
        _courseAccessService = courseAccessService;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<AccessRequestOutput> ExecuteAsync(
        RequestCourseAccessInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(input));
        }

        if (input.CourseId == Guid.Empty)
        {
            throw new ArgumentException("CourseId is required.", nameof(input));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var user = await _users.FindByIdAsync(input.UserId, cancellationToken);

            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }

            if (!user.Active || !user.EmailVerifiedAt.HasValue)
            {
                throw new ForbiddenException("User cannot request course access.");
            }

            var course = await _courses.FindByIdAsync(input.CourseId, cancellationToken);

            if (course is null || !course.Published)
            {
                throw new NotFoundException("Course not found.");
            }

            if (course.PricingModel == CoursePricingModel.Free)
            {
                throw new ConflictException("Course is free; no access request needed.");
            }

            var access = await _courseAccessService.CanUserAccessCourseAsync(
                input.UserId, input.CourseId, cancellationToken);

            if (access.CanAccess)
            {
                throw new ConflictException("User already has access to this course.");
            }

            var pending = await _accessRequests.FindPendingByUserAndCourseAsync(
                input.UserId, input.CourseId, cancellationToken);

            if (pending is not null)
            {
                throw new ConflictException("A pending access request already exists for this course.");
            }

            var request = AccessRequest.Create(input.UserId, input.CourseId);
            await _accessRequests.CreateAsync(request, cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.AccessRequestCreated,
                "AccessRequest",
                request.Id,
                new Dictionary<string, string?>
                {
                    ["courseId"] = request.CourseId.ToString()
                },
                userId: input.UserId,
                cancellationToken: cancellationToken);

            return AccessRequestOutput.FromAccessRequest(request);
        }, cancellationToken);
    }
}
