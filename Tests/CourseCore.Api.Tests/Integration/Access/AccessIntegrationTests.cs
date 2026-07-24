using System.Net;
using System.Net.Http.Json;
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
