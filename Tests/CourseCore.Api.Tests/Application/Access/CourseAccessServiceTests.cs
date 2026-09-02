using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class CourseAccessServiceTests
{
    [Fact]
    public async Task CanUserAccessCourseAsync_WhenEmailIsNotVerified_ShouldDenyAccessEvenWithGrant()
    {
        var fixture = CreateFixture(emailVerified: false);
        fixture.Areas.UserAreaAccesses.Add(UserAreaAccess.Create(fixture.UserId, fixture.AreaId, canView: true, canManage: false));

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenCourseIsFree_ShouldAllowAccessWithoutGrant()
    {
        var fixture = CreateFixture(pricingModel: CoursePricingModel.Free);

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.True(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenCourseIsFreeButEmailIsNotVerified_ShouldDenyAccess()
    {
        var fixture = CreateFixture(pricingModel: CoursePricingModel.Free, emailVerified: false);

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenCourseIsFreeAndUnpublished_ShouldDenyAccess()
    {
        var fixture = CreateFixture(pricingModel: CoursePricingModel.Free);
        var course = fixture.Courses.Courses.Single(c => c.Id == fixture.CourseId);
        course.Unpublish();

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
    }

    [Fact]
    public async Task ListCatalogAsync_ShouldMarkFreeAndGrantedCoursesAsAccessibleAndOthersAsLocked()
    {
        var fixture = CreateFixture();
        fixture.Areas.UserAreaAccesses.Add(UserAreaAccess.Create(fixture.UserId, fixture.AreaId, canView: true, canManage: false));
        var otherArea = TestEntityFactory.Area();
        var freeCourse = TestEntityFactory.PublishedCourse(otherArea.Id, CoursePricingModel.Free);
        var lockedCourse = TestEntityFactory.PublishedCourse(otherArea.Id, CoursePricingModel.Paid);
        fixture.Areas.Areas.Add(otherArea);
        fixture.Courses.Courses.Add(freeCourse);
        fixture.Courses.Courses.Add(lockedCourse);

        var entries = await fixture.Service.ListCatalogAsync(fixture.UserId);

        Assert.True(entries.Single(entry => entry.Course.Id == fixture.CourseId).HasAccess);
        Assert.True(entries.Single(entry => entry.Course.Id == freeCourse.Id).HasAccess);
        Assert.False(entries.Single(entry => entry.Course.Id == lockedCourse.Id).HasAccess);
    }

    [Fact]
    public async Task ListCatalogAsync_WhenEmailIsNotVerified_ShouldMarkEverythingAsLocked()
    {
        var fixture = CreateFixture(emailVerified: false, pricingModel: CoursePricingModel.Free);

        var entries = await fixture.Service.ListCatalogAsync(fixture.UserId);

        Assert.All(entries, entry => Assert.False(entry.HasAccess));
    }

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
    public async Task CanUserAccessCourseAsync_WhenUserHasInactiveRoleAreaAccess_ShouldDenyAccess()
    {
        var fixture = CreateFixture(roleActive: false);
        fixture.Areas.RoleAreaAccesses.Add(RoleAreaAccess.Create(fixture.RoleId, fixture.AreaId, canView: true, canManage: false));

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
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

    private static CourseAccessFixture CreateFixture(
        bool userActive = true,
        bool areaActive = true,
        bool roleActive = true,
        bool emailVerified = true,
        CoursePricingModel pricingModel = CoursePricingModel.Paid)
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var user = TestEntityFactory.User(userId, active: userActive, emailVerified: emailVerified);
        var role = TestEntityFactory.Role(roleId, active: roleActive);
        var area = TestEntityFactory.Area(areaId, areaActive);
        var course = TestEntityFactory.PublishedCourse(area.Id, pricingModel);

        users.Add(user);
        roles.AddForUser(user.Id, role);
        areas.Areas.Add(area);
        courses.Courses.Add(course);

        return new CourseAccessFixture(
            new CourseAccessService(users, roles, areas, courses),
            areas,
            courses,
            user.Id,
            role.Id,
            area.Id,
            course.Id);
    }

    private sealed record CourseAccessFixture(
        CourseAccessService Service,
        FakeAreaRepository Areas,
        FakeCourseRepository Courses,
        Guid UserId,
        Guid RoleId,
        Guid AreaId,
        Guid CourseId);
}
