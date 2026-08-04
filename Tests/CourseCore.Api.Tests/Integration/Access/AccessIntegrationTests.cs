using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Access.Presentation.Responses;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Access;

public class AccessIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public AccessIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GrantUserAreaAccess_WhenAdminPostsValidRequest_ShouldReturnOk()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var user = await _factory.SeedUserAsync();
        var areaId = await _factory.SeedAreaAsync();

        var response = await client.PostAsJsonAsync("/api/access/user-area", new
        {
            userId = user.Id,
            areaId,
            canView = true,
            canManage = false
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GrantRoleAreaAccess_WhenAdminPostsValidRequest_ShouldReturnOk()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var areaId = await _factory.SeedAreaAsync();
        var adminRoleId = await _factory.GetAdminRoleIdAsync();

        var response = await client.PostAsJsonAsync("/api/access/role-area", new
        {
            roleId = adminRoleId,
            areaId,
            canView = true,
            canManage = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GrantUserAreaAccess_WhenUserHasManageUsersPermission_ShouldReturnOk()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var operatorUser = await _factory.SeedUserAsync(AuthPermissionNames.ManageUsers);
        var targetUser = await _factory.SeedUserAsync();
        var areaId = await _factory.SeedAreaAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, operatorUser);

        var response = await client.PostAsJsonAsync("/api/access/user-area", new
        {
            userId = targetUser.Id,
            areaId,
            canView = true,
            canManage = false
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GrantUserAreaAccess_WhenUserOnlyHasManageRolesPermission_ShouldReturnForbidden()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var operatorUser = await _factory.SeedUserAsync(AuthPermissionNames.ManageRoles);
        await IntegrationAuth.AuthenticateAsAsync(client, operatorUser);

        var response = await client.PostAsJsonAsync("/api/access/user-area", new
        {
            userId = Guid.NewGuid(),
            areaId = Guid.NewGuid(),
            canView = true,
            canManage = false
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GrantRoleAreaAccess_WhenUserHasManageRolesPermission_ShouldReturnOk()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var operatorUser = await _factory.SeedUserAsync(AuthPermissionNames.ManageRoles);
        var areaId = await _factory.SeedAreaAsync();
        var roleId = await _factory.GetAdminRoleIdAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, operatorUser);

        var response = await client.PostAsJsonAsync("/api/access/role-area", new
        {
            roleId,
            areaId,
            canView = true,
            canManage = false
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GrantRoleAreaAccess_WhenUserOnlyHasManageUsersPermission_ShouldReturnForbidden()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var operatorUser = await _factory.SeedUserAsync(AuthPermissionNames.ManageUsers);
        await IntegrationAuth.AuthenticateAsAsync(client, operatorUser);

        var response = await client.PostAsJsonAsync("/api/access/role-area", new
        {
            roleId = Guid.NewGuid(),
            areaId = Guid.NewGuid(),
            canView = true,
            canManage = false
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CheckCourseAccess_WhenAdminIsAuthenticated_ShouldReturnOk()
    {
        var course = await _factory.SeedPublishedCourseAsync(grantAdminAccess: true);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/access/course/check", new
        {
            courseId = course
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CheckOwnCourseAccess_WhenCommonUserIsAuthenticated_ShouldReturnOwnAccess()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync($"/api/access/courses/{course.CourseId}");
        var body = await response.Content.ReadFromJsonAsync<CourseAccessResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(user.Id, body.UserId);
        Assert.True(body.CanAccess);
    }

    [Fact]
    public async Task CheckUserCourseAccess_WhenCommonUserRequestsAnotherUser_ShouldReturnForbidden()
    {
        var user = await _factory.SeedUserAsync();
        var targetUser = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(targetUser.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync($"/api/access/users/{targetUser.Id}/courses/{course.CourseId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CheckUserCourseAccess_WhenUserHasManageCoursesPermission_ShouldReturnTargetAccess()
    {
        var operatorUser = await _factory.SeedUserAsync(AuthPermissionNames.ManageCourses);
        var targetUser = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(targetUser.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, operatorUser);

        var response = await client.GetAsync($"/api/access/users/{targetUser.Id}/courses/{course.CourseId}");
        var body = await response.Content.ReadFromJsonAsync<CourseAccessResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(targetUser.Id, body.UserId);
        Assert.True(body.CanAccess);
    }

    [Fact]
    public async Task CheckUserCourseAccess_WhenAdminRequestsTargetUser_ShouldReturnOk()
    {
        var targetUser = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(targetUser.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync($"/api/access/users/{targetUser.Id}/courses/{course.CourseId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LegacyCheckCourseAccess_WhenCommonUserIsAuthenticated_ShouldCheckOwnAccessOnly()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/access/course/check", new
        {
            courseId = course.CourseId
        });
        var body = await response.Content.ReadFromJsonAsync<CourseAccessResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(user.Id, body.UserId);
    }

    [Fact]
    public async Task GrantUserAreaAccess_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync("/api/access/user-area", new
        {
            userId = Guid.NewGuid(),
            areaId = Guid.NewGuid(),
            canView = true,
            canManage = false
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GrantRoleAreaAccess_WhenUserHasNoPermissionOrAdminRole_ShouldReturnForbidden()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/access/role-area", new
        {
            roleId = Guid.NewGuid(),
            areaId = Guid.NewGuid(),
            canView = true,
            canManage = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
