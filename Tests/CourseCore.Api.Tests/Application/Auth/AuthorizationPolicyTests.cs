using System.Security.Claims;
using CourseCore.Api.Modules.Auth;
using CourseCore.Api.Modules.Auth.Application.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CourseCore.Api.Tests.Application.Auth;

public class AuthorizationPolicyTests
{
    [Fact]
    public async Task ManageUsers_WhenUserHasPermissionWithoutAdminRole_ShouldSucceed()
    {
        var result = await AuthorizeAsync(
            AuthPolicyNames.ManageUsers,
            CreatePrincipal(new Claim(AuthClaimTypes.Permission, AuthPermissionNames.ManageUsers)));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ManageUsers_WhenUserHasAdminRoleWithoutPermission_ShouldSucceed()
    {
        var result = await AuthorizeAsync(
            AuthPolicyNames.ManageUsers,
            CreatePrincipal(new Claim(AuthClaimTypes.Role, AuthRoleNames.Admin)));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ManageUsers_WhenUserHasNoPermissionOrAdminRole_ShouldFail()
    {
        var result = await AuthorizeAsync(
            AuthPolicyNames.ManageUsers,
            CreatePrincipal(new Claim(AuthClaimTypes.Permission, AuthPermissionNames.ManageCourses)));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ManageUserAreaAccess_WhenUserHasManageUsersPermission_ShouldSucceed()
    {
        var result = await AuthorizeAsync(
            AuthPolicyNames.ManageUserAreaAccess,
            CreatePrincipal(new Claim(AuthClaimTypes.Permission, AuthPermissionNames.ManageUsers)));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ManageUserAreaAccess_WhenUserOnlyHasManageRolesPermission_ShouldFail()
    {
        var result = await AuthorizeAsync(
            AuthPolicyNames.ManageUserAreaAccess,
            CreatePrincipal(new Claim(AuthClaimTypes.Permission, AuthPermissionNames.ManageRoles)));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ManageRoleAreaAccess_WhenUserHasManageRolesPermission_ShouldSucceed()
    {
        var result = await AuthorizeAsync(
            AuthPolicyNames.ManageRoleAreaAccess,
            CreatePrincipal(new Claim(AuthClaimTypes.Permission, AuthPermissionNames.ManageRoles)));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ManageRoleAreaAccess_WhenUserOnlyHasManageUsersPermission_ShouldFail()
    {
        var result = await AuthorizeAsync(
            AuthPolicyNames.ManageRoleAreaAccess,
            CreatePrincipal(new Claim(AuthClaimTypes.Permission, AuthPermissionNames.ManageUsers)));

        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData(AuthPolicyNames.ManageUserAreaAccess)]
    [InlineData(AuthPolicyNames.ManageRoleAreaAccess)]
    public async Task AccessGrantPolicies_WhenUserOnlyHasManageAreasPermission_ShouldFail(string policy)
    {
        var result = await AuthorizeAsync(
            policy,
            CreatePrincipal(new Claim(AuthClaimTypes.Permission, AuthPermissionNames.ManageAreas)));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task CheckOwnCourseAccess_WhenUserIsAuthenticated_ShouldSucceed()
    {
        var result = await AuthorizeAsync(
            AuthPolicyNames.CheckOwnCourseAccess,
            CreatePrincipal());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(AuthPermissionNames.ManageUsers)]
    [InlineData(AuthPermissionNames.ManageAreas)]
    [InlineData(AuthPermissionNames.ManageCourses)]
    public async Task CheckUserCourseAccess_WhenUserHasAdministrativePermission_ShouldSucceed(string permission)
    {
        var result = await AuthorizeAsync(
            AuthPolicyNames.CheckUserCourseAccess,
            CreatePrincipal(new Claim(AuthClaimTypes.Permission, permission)));

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(AuthPolicyNames.ManageUserAreaAccess)]
    [InlineData(AuthPolicyNames.ManageRoleAreaAccess)]
    [InlineData(AuthPolicyNames.CheckUserCourseAccess)]
    public async Task AccessAdministrationPolicies_WhenUserIsAdmin_ShouldSucceed(string policy)
    {
        var result = await AuthorizeAsync(
            policy,
            CreatePrincipal(new Claim(AuthClaimTypes.Role, AuthRoleNames.Admin)));

        Assert.True(result.Succeeded);
    }

    private static async Task<AuthorizationResult> AuthorizeAsync(string policyName, ClaimsPrincipal user)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthModule(CreateConfiguration());
        using var serviceProvider = services.BuildServiceProvider();
        var authorization = serviceProvider.GetRequiredService<IAuthorizationService>();

        return await authorization.AuthorizeAsync(user, resource: null, policyName);
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "CourseCore.UnitTests",
                ["Jwt:Audience"] = "CourseCore.UnitTests",
                ["Jwt:SecretKey"] = "unit-test-secret-key-32-characters-minimum",
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            })
            .Build();
    }
}
