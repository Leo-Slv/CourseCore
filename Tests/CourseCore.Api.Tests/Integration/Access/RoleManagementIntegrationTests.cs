using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Modules.Access.Presentation.Requests;
using CourseCore.Api.Modules.Access.Presentation.Responses;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Access;

public class RoleManagementIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public RoleManagementIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/admin/roles")]
    [InlineData("/api/admin/permissions")]
    public async Task AdminEndpoints_WhenStaffHasEveryOtherPermission_ShouldReturnForbidden(string url)
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync(AuthPermissionNames.ManageRoles);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoints_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.GetAsync("/api/admin/roles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListPermissions_WhenAdmin_ShouldReturnTheCatalog()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var body = await client.GetFromJsonAsync<List<PermissionResponse>>("/api/admin/permissions");

        Assert.NotNull(body);
        Assert.Contains(body!, permission => permission.Key == AuthPermissionNames.ReadVisitors);
        Assert.Contains(body!, permission => permission.Key == AuthPermissionNames.ManageCourses);
    }

    [Fact]
    public async Task ListRoles_WhenAdmin_ShouldFlagAdminAsSystemAndIncludeInactiveRoles()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var inactiveId = await _factory.SeedRoleAsync($"inactive-{Guid.NewGuid():N}", active: false);

        var body = await client.GetFromJsonAsync<List<RoleDetailResponse>>("/api/admin/roles");

        Assert.NotNull(body);
        var admin = Assert.Single(body!, role => role.Name == AuthRoleNames.Admin);
        Assert.True(admin.IsSystem);
        Assert.Contains(body!, role => role.Id == inactiveId && !role.Active);
    }

    [Fact]
    public async Task RoleLifecycle_CreateGrantRevokeAndSignOutMembers()
    {
        using var admin = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(admin);
        var roleName = $"Secretaria {Guid.NewGuid():N}";

        var created = await admin.PostAsJsonAsync(
            "/api/admin/roles",
            new CreateRoleRequest { Name = roleName, Description = "Equipe" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var role = await created.Content.ReadFromJsonAsync<RoleDetailResponse>();

        var staff = await _factory.SeedUserAsync();
        var assign = await admin.PostAsync($"/api/users/{staff.Id}/roles/{role!.Id}", content: null);
        Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);

        var grant = await admin.PutAsJsonAsync(
            $"/api/admin/roles/{role.Id}/permissions",
            new SetRolePermissionsRequest { PermissionKeys = [AuthPermissionNames.ReadVisitors] });
        Assert.Equal(HttpStatusCode.OK, grant.StatusCode);
        var granted = await grant.Content.ReadFromJsonAsync<RoleDetailResponse>();
        Assert.Equal([AuthPermissionNames.ReadVisitors], granted!.PermissionKeys);

        using var staffClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(staffClient, staff);
        Assert.Equal(HttpStatusCode.OK, (await staffClient.GetAsync("/api/visitors")).StatusCode);

        var revoke = await admin.PutAsJsonAsync(
            $"/api/admin/roles/{role.Id}/permissions",
            new SetRolePermissionsRequest { PermissionKeys = [] });
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

        // The old access token is rejected right away (token version bumped)...
        Assert.Equal(HttpStatusCode.Unauthorized, (await staffClient.GetAsync("/api/visitors")).StatusCode);

        // ...and a fresh login no longer carries the permission.
        using var relogged = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(relogged, staff);
        Assert.Equal(HttpStatusCode.Forbidden, (await relogged.GetAsync("/api/visitors")).StatusCode);
    }

    [Fact]
    public async Task CreateRole_WhenNameExists_ShouldReturnConflict()
    {
        using var admin = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(admin);
        var name = $"Dup {Guid.NewGuid():N}";
        await _factory.SeedRoleAsync(name);

        var response = await admin.PostAsJsonAsync("/api/admin/roles", new CreateRoleRequest { Name = name });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SetPermissions_WhenKeyIsUnknown_ShouldReturnBadRequest()
    {
        using var admin = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(admin);
        var roleId = await _factory.SeedRoleAsync($"r-{Guid.NewGuid():N}");

        var response = await admin.PutAsJsonAsync(
            $"/api/admin/roles/{roleId}/permissions",
            new SetRolePermissionsRequest { PermissionKeys = ["nope.nope"] });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRole_WhenAdminRole_ShouldReturnForbidden()
    {
        using var admin = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(admin);
        var adminRoleId = await _factory.GetAdminRoleIdAsync();

        var response = await admin.PutAsJsonAsync(
            $"/api/admin/roles/{adminRoleId}",
            new UpdateRoleRequest { Name = "Admin", Active = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignRole_WhenCallerOnlyHasManageUsers_ShouldReturnForbidden()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var staff = await _factory.SeedUserAsync(AuthPermissionNames.ManageUsers);
        await IntegrationAuth.AuthenticateAsAsync(client, staff);
        var adminRoleId = await _factory.GetAdminRoleIdAsync();

        var response = await client.PostAsync($"/api/users/{staff.Id}/roles/{adminRoleId}", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
