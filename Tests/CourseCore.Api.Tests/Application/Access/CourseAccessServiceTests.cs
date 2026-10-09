using CourseCore.Api.Modules.Access.Application.Services;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class CourseAccessServiceTests
{
    [Fact]
    public async Task CanUserAccessCourseAsync_WhenEmailIsNotVerified_ShouldDenyAccessEvenWithGrant()
    {
        var fixture = CreateFixture(emailVerified: false);
        fixture.Grant();

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenCourseIsFreeInNonPublicAreaWithoutGrant_ShouldDenyAccess()
    {
        var fixture = CreateFixture(pricingModel: CoursePricingModel.Free);

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenCourseIsFreeInPublicArea_ShouldAllowAccessWithoutGrant()
    {
        var fixture = CreateFixture(pricingModel: CoursePricingModel.Free, areaIsPublic: true);

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.True(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenCourseIsPaidInPublicAreaWithoutGrant_ShouldDenyAccess()
    {
        var fixture = CreateFixture(pricingModel: CoursePricingModel.Paid, areaIsPublic: true);

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenCourseIsFreeInPublicAreaButEmailIsNotVerified_ShouldDenyAccess()
    {
        var fixture = CreateFixture(pricingModel: CoursePricingModel.Free, areaIsPublic: true, emailVerified: false);

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenCourseIsFreeInPublicAreaAndUnpublished_ShouldDenyAccess()
    {
        var fixture = CreateFixture(pricingModel: CoursePricingModel.Free, areaIsPublic: true);
        var course = fixture.Courses.Courses.Single(c => c.Id == fixture.CourseId);
        course.Unpublish();

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenCourseIsFreeInNonPublicAreaButUserHasCourseAccess_ShouldAllowAccess()
    {
        var fixture = CreateFixture(pricingModel: CoursePricingModel.Free);
        fixture.Grant();

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.True(output.CanAccess);
    }

    [Fact]
    public async Task ListCatalogAsync_ShouldMarkGrantedAndPublicAreaFreeCoursesAsAccessibleAndOthersAsLocked()
    {
        var fixture = CreateFixture();
        fixture.Grant();
        var publicArea = TestEntityFactory.Area(isPublic: true);
        var privateArea = TestEntityFactory.Area();
        var freeCourseInPublicArea = TestEntityFactory.PublishedCourse(publicArea.Id, CoursePricingModel.Free);
        var freeCourseInPrivateArea = TestEntityFactory.PublishedCourse(privateArea.Id, CoursePricingModel.Free);
        var lockedPaidCourseInPublicArea = TestEntityFactory.PublishedCourse(publicArea.Id, CoursePricingModel.Paid);
        fixture.Areas.Areas.Add(publicArea);
        fixture.Areas.Areas.Add(privateArea);
        fixture.Courses.Courses.Add(freeCourseInPublicArea);
        fixture.Courses.Courses.Add(freeCourseInPrivateArea);
        fixture.Courses.Courses.Add(lockedPaidCourseInPublicArea);

        var entries = await fixture.Service.ListCatalogAsync(fixture.UserId);

        Assert.True(entries.Single(entry => entry.Course.Id == fixture.CourseId).HasAccess);
        Assert.True(entries.Single(entry => entry.Course.Id == freeCourseInPublicArea.Id).HasAccess);
        Assert.False(entries.Single(entry => entry.Course.Id == freeCourseInPrivateArea.Id).HasAccess);
        Assert.False(entries.Single(entry => entry.Course.Id == lockedPaidCourseInPublicArea.Id).HasAccess);
    }

    [Fact]
    public async Task ListCatalogAsync_WhenEmailIsNotVerified_ShouldMarkEverythingAsLocked()
    {
        var fixture = CreateFixture(emailVerified: false, pricingModel: CoursePricingModel.Free, areaIsPublic: true);

        var entries = await fixture.Service.ListCatalogAsync(fixture.UserId);

        Assert.All(entries, entry => Assert.False(entry.HasAccess));
    }

    [Fact]
    public async Task ListCatalogAsync_WhenUserHasGrantForOneCourse_ShouldNotUnlockOtherPaidCoursesInSameArea()
    {
        var fixture = CreateFixture();
        fixture.Grant();
        var otherPaidCourse = TestEntityFactory.PublishedCourse(fixture.AreaId, CoursePricingModel.Paid);
        fixture.Courses.Courses.Add(otherPaidCourse);

        var entries = await fixture.Service.ListCatalogAsync(fixture.UserId);

        Assert.True(entries.Single(entry => entry.Course.Id == fixture.CourseId).HasAccess);
        Assert.False(entries.Single(entry => entry.Course.Id == otherPaidCourse.Id).HasAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenUserHasCourseAccess_ShouldAllowAccess()
    {
        var fixture = CreateFixture();
        fixture.Grant();

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.True(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenUserHasGrantForAnotherCourseInSameArea_ShouldDenyAccess()
    {
        var fixture = CreateFixture();
        var otherPaidCourse = TestEntityFactory.PublishedCourse(fixture.AreaId, CoursePricingModel.Paid);
        fixture.Courses.Courses.Add(otherPaidCourse);
        fixture.Grant(otherPaidCourse.Id);

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
        Assert.Equal(CourseAccessDenialReasons.NoCourseAccessFound, output.Reason);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenUserHasAdminRole_ShouldAllowAccess()
    {
        var fixture = CreateFixture(roleName: AuthRoleNames.Admin);

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.True(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenUserHasManageCoursesPermission_ShouldAllowAccess()
    {
        var fixture = CreateFixture();
        fixture.Roles.AddPermissionForUser(fixture.UserId, AuthPermissionNames.ManageCourses);

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.True(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenAdminRoleIsInactive_ShouldDenyAccess()
    {
        var fixture = CreateFixture(roleName: AuthRoleNames.Admin, roleActive: false);

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
    }

    [Fact]
    public async Task ListCatalogAsync_WhenUserIsStaff_ShouldMarkPaidCoursesAsAccessible()
    {
        var fixture = CreateFixture(roleName: AuthRoleNames.Admin);

        var entries = await fixture.Service.ListCatalogAsync(fixture.UserId);

        Assert.True(entries.Single(entry => entry.Course.Id == fixture.CourseId).HasAccess);
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
        fixture.Grant();

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
    }

    [Fact]
    public async Task CanUserAccessCourseAsync_WhenAreaIsInactive_ShouldDenyAccess()
    {
        var fixture = CreateFixture(areaActive: false);
        fixture.Grant();

        var output = await fixture.Service.CanUserAccessCourseAsync(fixture.UserId, fixture.CourseId);

        Assert.False(output.CanAccess);
    }

    private static CourseAccessFixture CreateFixture(
        bool userActive = true,
        bool areaActive = true,
        bool roleActive = true,
        bool emailVerified = true,
        bool areaIsPublic = false,
        CoursePricingModel pricingModel = CoursePricingModel.Paid,
        string roleName = "Student")
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        var courses = new FakeCourseRepository();
        var courseAccesses = new FakeUserCourseAccessRepository();
        var user = TestEntityFactory.User(userId, active: userActive, emailVerified: emailVerified);
        var role = TestEntityFactory.Role(roleId, roleName, active: roleActive);
        var area = TestEntityFactory.Area(areaId, areaActive, areaIsPublic);
        var course = TestEntityFactory.PublishedCourse(area.Id, pricingModel);

        users.Add(user);
        roles.AddForUser(user.Id, role);
        areas.Areas.Add(area);
        courses.Courses.Add(course);

        return new CourseAccessFixture(
            new CourseAccessService(users, roles, areas, courses, courseAccesses),
            areas,
            courses,
            roles,
            courseAccesses,
            user.Id,
            area.Id,
            course.Id);
    }

    private sealed record CourseAccessFixture(
        CourseAccessService Service,
        FakeAreaRepository Areas,
        FakeCourseRepository Courses,
        FakeRoleRepository Roles,
        FakeUserCourseAccessRepository CourseAccesses,
        Guid UserId,
        Guid AreaId,
        Guid CourseId)
    {
        public void Grant(Guid? courseId = null)
        {
            CourseAccesses.Accesses.Add(
                UserCourseAccess.Create(UserId, courseId ?? CourseId, CourseAccessSource.Manual));
        }
    }
}
