using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Users.Domain.Repositories;

namespace CourseCore.Api.Modules.Access.Application.Services;

public class CourseAccessService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IAreaRepository _areas;
    private readonly ICourseRepository _courses;
    private readonly IUserCourseAccessRepository _userCourseAccesses;

    public CourseAccessService(
        IUserRepository users,
        IRoleRepository roles,
        IAreaRepository areas,
        ICourseRepository courses,
        IUserCourseAccessRepository userCourseAccesses)
    {
        _users = users;
        _roles = roles;
        _areas = areas;
        _courses = courses;
        _userCourseAccesses = userCourseAccesses;
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

        if (!user.EmailVerifiedAt.HasValue)
        {
            return Denied(userId, courseId, "Email is not verified.");
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
            return Denied(userId, courseId, CourseAccessDenialReasons.CourseHasNoLinkedAreas);
        }

        var activeCourseAreaIds = await GetActiveAreaIdsAsync(courseAreaIds, cancellationToken);

        if (activeCourseAreaIds.Count == 0)
        {
            return Denied(userId, courseId, CourseAccessDenialReasons.CourseHasNoActiveLinkedAreas);
        }

        if (course.PricingModel == CoursePricingModel.Free
            && await HasPublicAreaAsync(activeCourseAreaIds, cancellationToken))
        {
            return Allowed(userId, courseId, "Access granted by public area free course.");
        }

        if (await _userCourseAccesses.ExistsAsync(userId, courseId, cancellationToken))
        {
            return Allowed(userId, courseId, "Access granted by user course access.");
        }

        if (await IsCourseStaffAsync(userId, cancellationToken))
        {
            return Allowed(userId, courseId, "Access granted by course management permission.");
        }

        return Denied(userId, courseId, CourseAccessDenialReasons.NoCourseAccessFound);
    }

    public async Task<IReadOnlyCollection<AreaOutput>> ListActiveAreasAsync(CancellationToken cancellationToken = default)
    {
        var areas = await _areas.ListAsync(cancellationToken);

        return areas.Where(area => area.Active).Select(area => AreaOutput.FromArea(area)).ToList();
    }

    public async Task<IReadOnlyCollection<CourseCatalogEntry>> ListCatalogAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var courses = await _courses.ListPublishedAsync(cancellationToken);
        var user = await _users.FindByIdAsync(userId, cancellationToken);

        if (user is null || !user.Active || !user.EmailVerifiedAt.HasValue)
        {
            return courses.Select(course => new CourseCatalogEntry(course, HasAccess: false)).ToList();
        }

        var allAreas = await _areas.ListAsync(cancellationToken);
        var activeAreaIds = allAreas.Where(area => area.Active).Select(area => area.Id).ToHashSet();
        var publicAreaIds = allAreas.Where(area => area.Active && area.IsPublic).Select(area => area.Id).ToHashSet();
        var grantedCourseIds = (await _userCourseAccesses.ListByUserIdAsync(userId, cancellationToken))
            .Select(access => access.CourseId)
            .ToHashSet();
        var isStaff = await IsCourseStaffAsync(userId, cancellationToken);

        return courses
            .Select(course => new CourseCatalogEntry(
                course,
                HasCatalogAccess(course, activeAreaIds, publicAreaIds, grantedCourseIds, isStaff)))
            .ToList();
    }

    private static bool HasCatalogAccess(
        Course course,
        HashSet<Guid> activeAreaIds,
        HashSet<Guid> publicAreaIds,
        HashSet<Guid> grantedCourseIds,
        bool isStaff)
    {
        if (!course.AreaIds.Any(activeAreaIds.Contains))
        {
            return false;
        }

        if (isStaff || grantedCourseIds.Contains(course.Id))
        {
            return true;
        }

        return course.PricingModel == CoursePricingModel.Free && course.AreaIds.Any(publicAreaIds.Contains);
    }

    private async Task<bool> HasPublicAreaAsync(HashSet<Guid> areaIds, CancellationToken cancellationToken)
    {
        var areas = await _areas.ListAsync(cancellationToken);

        return areas.Any(area => areaIds.Contains(area.Id) && area.IsPublic);
    }

    private async Task<bool> IsCourseStaffAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roles = await _roles.FindByUserIdAsync(userId, cancellationToken);

        if (roles.Any(role => string.Equals(role.Name, AuthRoleNames.Admin, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var permissions = await _roles.FindPermissionKeysByUserIdAsync(userId, cancellationToken);

        return permissions.Contains(AuthPermissionNames.ManageCourses);
    }

    private async Task<HashSet<Guid>> GetActiveAreaIdsAsync(
        HashSet<Guid> areaIds,
        CancellationToken cancellationToken)
    {
        var areas = await _areas.ListAsync(cancellationToken);

        return areas
            .Where(area => area.Active && areaIds.Contains(area.Id))
            .Select(area => area.Id)
            .ToHashSet();
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
