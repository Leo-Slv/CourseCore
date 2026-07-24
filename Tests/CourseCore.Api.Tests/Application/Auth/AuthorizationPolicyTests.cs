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
    public async Task ManageAccess_WhenUserHasAreaPermissionWithoutAdminRole_ShouldSucceed()
    {
        var result = await AuthorizeAsync(
            AuthPolicyNames.ManageAccess,
            CreatePrincipal(new Claim(AuthClaimTypes.Permission, AuthPermissionNames.ManageAreas)));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ManageAccess_WhenUserHasRolePermissionWithoutAdminRole_ShouldSucceed()
    {
        var result = await AuthorizeAsync(
            AuthPolicyNames.ManageAccess,
            CreatePrincipal(new Claim(AuthClaimTypes.Permission, AuthPermissionNames.ManageRoles)));

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
