using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class RevokeUserCourseAccessUseCase
{
    private readonly IUserCourseAccessRepository _accesses;
    private readonly IUserRepository _users;
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public RevokeUserCourseAccessUseCase(
        IUserCourseAccessRepository accesses,
        IUserRepository users,
        ICourseRepository courses,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _accesses = accesses;
        _users = users;
        _courses = courses;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task ExecuteAsync(Guid userId, Guid courseId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        if (courseId == Guid.Empty)
        {
            throw new ArgumentException("CourseId is required.", nameof(courseId));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var access = await _accesses.FindAsync(userId, courseId, cancellationToken);

            if (access is null)
            {
                throw new NotFoundException("User course access not found.");
            }

            await _accesses.RemoveAsync(access, cancellationToken);

            var user = await _users.FindByIdAsync(userId, cancellationToken);
            var course = await _courses.FindByIdAsync(courseId, cancellationToken);

            await _auditLogs.RecordAsync(
                AuditLogActionNames.UserCourseAccessRevoked,
                "UserCourseAccess",
                access.Id,
                new Dictionary<string, string?>
                {
                    ["targetUserId"] = userId.ToString(),
                    ["courseId"] = courseId.ToString(),
                    ["displayName"] = $"{user?.Email.Value ?? userId.ToString()} → {course?.Title ?? courseId.ToString()}"
                },
                cancellationToken: cancellationToken);
        }, cancellationToken);
    }
}
