using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;

namespace CourseCore.Api.Modules.Access.Application.Services;

public class CourseAccessService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IAreaRepository _areas;
    private readonly ICourseRepository _courses;

    public CourseAccessService(
        IUserRepository users,
        IRoleRepository roles,
        IAreaRepository areas,
        ICourseRepository courses)
    {
        _users = users;
        _roles = roles;
        _areas = areas;
        _courses = courses;
    }

    public async Task<CourseAccessOutput> CanUserAccessCourseAsync(
        Guid userId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || courseId == Guid.Empty)
        {
            return Denied(userId, courseId, "Invalid access check.");
        }

        var user = await _users.FindByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Denied(userId, courseId, "User not found.");
        }

        if (!user.Active)
        {
            return Denied(userId, courseId, "User is inactive.");
        }

        var course = await _courses.FindDetailsByIdAsync(courseId, cancellationToken)
            ?? await _courses.FindByIdAsync(courseId, cancellationToken);

        if (course is null)
        {
            return Denied(userId, courseId, "Course not found.");
        }

        if (!course.Published)
        {
            return Denied(userId, courseId, "Course is not published.");
        }

        var courseAreaIds = course.AreaIds.ToHashSet();

        if (courseAreaIds.Count == 0)
        {
            return Denied(userId, courseId, "Course has no linked areas.");
        }

        var activeCourseAreaIds = await GetActiveCourseAreaIdsAsync(courseAreaIds, cancellationToken);

        if (activeCourseAreaIds.Count == 0)
        {
            return Denied(userId, courseId, "Course has no active linked areas.");
        }

        var now = DateTime.UtcNow;
        var userAccesses = await _areas.ListUserAreaAccessesAsync(userId, cancellationToken);

        if (userAccesses.Any(access => activeCourseAreaIds.Contains(access.AreaId) && access.IsValidAt(now)))
        {
            return Allowed(userId, courseId, "Access granted by user area access.");
        }

        var roles = await _roles.FindByUserIdAsync(userId, cancellationToken);
        var roleIds = roles.Select(role => role.Id).ToArray();
        var roleAccesses = await _areas.ListRoleAreaAccessesAsync(roleIds, cancellationToken);

        if (roleAccesses.Any(access => activeCourseAreaIds.Contains(access.AreaId) && HasPermission(access)))
        {
            return Allowed(userId, courseId, "Access granted by role area access.");
        }

        return Denied(userId, courseId, "No area access found.");
    }

    private async Task<HashSet<Guid>> GetActiveCourseAreaIdsAsync(
        HashSet<Guid> courseAreaIds,
        CancellationToken cancellationToken)
    {
        var areas = await _areas.ListAsync(cancellationToken);

        return areas
            .Where(area => area.Active && courseAreaIds.Contains(area.Id))
            .Select(area => area.Id)
            .ToHashSet();
    }

    private static bool HasPermission(RoleAreaAccess access)
    {
        return access.CanView || access.CanManage;
    }

    private static CourseAccessOutput Allowed(Guid userId, Guid courseId, string reason)
    {
        return new CourseAccessOutput
        {
            UserId = userId,
            CourseId = courseId,
            CanAccess = true,
            Reason = reason
        };
    }

    private static CourseAccessOutput Denied(Guid userId, Guid courseId, string reason)
    {
        return new CourseAccessOutput
        {
            UserId = userId,
            CourseId = courseId,
            CanAccess = false,
            Reason = reason
        };
    }
}
