using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class CourseAccessServiceTests
{
    [Fact]
    public async Task CanUserAccessCourseAsync_WhenUserHasValidAreaAccess_ShouldAllowAccess()
    {
        var fixture = CreateFixture();
        fixture.Areas.UserAreaAccesses.Add(UserAreaAccess.Create(fixture.UserId, fixture.AreaId, canView: true, canManage: false));

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.True(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenUserHasRoleAreaAccess_ShouldAllowAccess()
    {
        var fixture = CreateFixture();
        fixture.Areas.RoleAreaAccesses.Add(RoleAreaAccess.Create(fixture.RoleId, fixture.AreaId, canView: true, canManage: false));

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.True(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenUserHasNoAccess_ShouldDenyAccess()
    {
        var fixture = CreateFixture();

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenUserIsInactive_ShouldDenyAccess()
    {
        var fixture = CreateFixture(userActive: false);
        fixture.Areas.UserAreaAccesses.Add(UserAreaAccess.Create(fixture.UserId, fixture.AreaId, canView: true, canManage: false));

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenAreaIsInactive_ShouldDenyAccess()
    {
        var fixture = CreateFixture(areaActive: false);
        fixture.Areas.UserAreaAccesses.Add(UserAreaAccess.Create(fixture.UserId, fixture.AreaId, canView: true, canManage: false));

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
    }

    private static CourseAccessFixture CreateFixture(bool userActive = true, bool areaActive = true)
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var user = TestEntityFactory.User(userId, active: userActive);
        var role = TestEntityFactory.Role(roleId);
        var area = TestEntityFactory.Area(areaId, areaActive);
        var course = TestEntityFactory.PublishedCourse(area.Id);

        users.Add(user);
        roles.AddForUser(user.Id, role);
        areas.Areas.Add(area);
        courses.Courses.Add(course);

        return new CourseAccessFixture(
            new CourseAccessService(users, roles, areas, courses),
            areas,
            user.Id,
            role.Id,
            area.Id,
            course.Id);
    }

    private sealed record CourseAccessFixture(
        CourseAccessService Service,
        FakeAreaRepository Areas,
        Guid UserId,
        Guid RoleId,
        Guid AreaId,
        Guid CourseId);
}
