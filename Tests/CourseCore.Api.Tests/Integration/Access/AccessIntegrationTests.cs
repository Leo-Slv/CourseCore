using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Access.Presentation.Responses;
using CourseCore.Api.Modules.Courses.Domain.Enums;
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

    [Fact]
    public async Task RequestCourseAccess_WhenLockedPaidCourse_ShouldReturnOk()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/access/requests", new { courseId = course.CourseId });
        var body = await response.Content.ReadFromJsonAsync<AccessRequestResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Pending", body.Status);
    }

    [Fact]
    public async Task RequestCourseAccess_WhenCourseIsFree_ShouldReturnConflict()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(pricingModel: CoursePricingModel.Free);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/access/requests", new { courseId = course.CourseId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RequestCourseAccess_WhenUserAlreadyHasAccess_ShouldReturnConflict()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/access/requests", new { courseId = course.CourseId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RequestCourseAccess_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync("/api/access/requests", new { courseId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListAccessRequests_WhenAdmin_ShouldReturnPendingRequests()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        using var requesterClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(requesterClient, user);
        var created = await requesterClient.PostAsJsonAsync("/api/access/requests", new { courseId = course.CourseId });
        var createdBody = await created.Content.ReadFromJsonAsync<AccessRequestResponse>();

        using var adminClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);

        var response = await adminClient.GetAsync("/api/access/requests?status=Pending");
        var body = await response.Content.ReadFromJsonAsync<List<AccessRequestResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.NotNull(createdBody);
        Assert.Contains(body, r => r.Id == createdBody.Id);
    }

    [Fact]
    public async Task ListAccessRequests_WhenUserLacksPermission_ShouldReturnForbidden()
    {
        var user = await _factory.SeedUserAsync();
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync("/api/access/requests");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ApproveAccessRequest_WhenAdmin_ShouldGrantUserAreaAccess()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        using var requesterClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(requesterClient, user);
        var created = await requesterClient.PostAsJsonAsync("/api/access/requests", new { courseId = course.CourseId });
        var createdBody = await created.Content.ReadFromJsonAsync<AccessRequestResponse>();

        using var adminClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);

        var approveResponse = await adminClient.PostAsync($"/api/access/requests/{createdBody!.Id}/approve", content: null);
        var approveBody = await approveResponse.Content.ReadFromJsonAsync<AccessRequestResponse>();

        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        Assert.NotNull(approveBody);
        Assert.Equal("Approved", approveBody.Status);

        var accessResponse = await requesterClient.GetAsync($"/api/access/courses/{course.CourseId}");
        var accessBody = await accessResponse.Content.ReadFromJsonAsync<CourseAccessResponse>();

        Assert.NotNull(accessBody);
        Assert.True(accessBody.CanAccess);
    }

    [Fact]
    public async Task RejectAccessRequest_WhenAdmin_ShouldMarkRejected()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        using var requesterClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(requesterClient, user);
        var created = await requesterClient.PostAsJsonAsync("/api/access/requests", new { courseId = course.CourseId });
        var createdBody = await created.Content.ReadFromJsonAsync<AccessRequestResponse>();

        using var adminClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);

        var response = await adminClient.PostAsync($"/api/access/requests/{createdBody!.Id}/reject", content: null);
        var body = await response.Content.ReadFromJsonAsync<AccessRequestResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Rejected", body.Status);
    }
}
