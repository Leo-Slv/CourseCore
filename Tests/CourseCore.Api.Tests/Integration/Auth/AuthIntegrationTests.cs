using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CourseCore.Api.Tests.Integration.Auth;

public class AuthIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public AuthIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUsers_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenAdminCredentialsAreValid_ShouldReturnAccessTokenAndRefreshToken()
    {
        using var client = CreateClient();

        var login = await LoginAsync(client);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(login.RefreshToken));
    }

    [Fact]
    public async Task Login_WhenAdminRoleIsActive_ShouldReturnAdminRoleClaim()
    {
        using var client = CreateClient();

        var login = await LoginAsync(client);
        var roles = ReadRoleClaims(login.AccessToken);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(AuthRoleNames.Admin, roles);
    }

    [Fact]
    public async Task Login_WhenAdminRoleIsInactive_ShouldNotReturnAdminRoleOrPermissionClaims()
    {
        using var factory = new CourseCoreApiFactory();
        await factory.SetAdminRoleActiveAsync(false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        var login = await LoginAsync(client);
        var roles = ReadRoleClaims(login.AccessToken);
        var permissions = ReadPermissionClaims(login.AccessToken);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.DoesNotContain(AuthRoleNames.Admin, roles);
        Assert.Empty(permissions);
    }

    [Fact]
    public async Task GetUsers_WhenAdminRoleIsInactive_ShouldReturnForbidden()
    {
        using var factory = new CourseCoreApiFactory();
        await factory.SetAdminRoleActiveAsync(false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        var login = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenAdminCredentialsAreValid_ShouldReturnPermissionClaims()
    {
        using var client = CreateClient();

        var login = await LoginAsync(client);
        var permissions = ReadPermissionClaims(login.AccessToken);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(AuthPermissionNames.ManageUsers, permissions);
        Assert.Contains(AuthPermissionNames.ManageRoles, permissions);
        Assert.Contains(AuthPermissionNames.ManageAreas, permissions);
        Assert.Contains(AuthPermissionNames.ManageCourses, permissions);
        Assert.Contains(AuthPermissionNames.ManageVideos, permissions);
        Assert.Contains(AuthPermissionNames.ReadProgress, permissions);
    }

    [Fact]
    public async Task Login_WhenUserRoleWithPermissionIsInactive_ShouldNotReturnPermissionClaim()
    {
        using var client = CreateClient();
        var user = await _factory.SeedUserWithRoleAsync(
            AuthPermissionNames.ManageUsers,
            roleActive: false);

        var login = await IntegrationAuth.LoginAsync(client, user);
        var permissions = ReadPermissionClaims(login.AccessToken);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.DoesNotContain(AuthPermissionNames.ManageUsers, permissions);
    }

    [Fact]
    public async Task GetUsers_WhenAdminTokenIsValid_ShouldReturnOk()
    {
        using var client = CreateClient();
        var login = await LoginAsync(client);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.GetAsync("/api/users");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected OK but received {(int)response.StatusCode}. WWW-Authenticate: {string.Join("; ", response.Headers.WwwAuthenticate)}");
    }

    [Fact]
    public async Task RefreshToken_WhenRefreshTokenIsValid_ShouldReturnNewTokens()
    {
        using var client = CreateClient();
        var login = await LoginAsync(client);

        var refresh = await RefreshAsync(client, login.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(refresh.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refresh.RefreshToken));
        Assert.NotEqual(login.RefreshToken, refresh.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_WhenRefreshTokenIsValid_ShouldReturnPermissionClaims()
    {
        using var client = CreateClient();
        var login = await LoginAsync(client);

        var refresh = await RefreshAsync(client, login.RefreshToken);
        var permissions = ReadPermissionClaims(refresh.AccessToken);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.Contains(AuthPermissionNames.ManageUsers, permissions);
        Assert.Contains(AuthPermissionNames.ManageCourses, permissions);
        Assert.Contains(AuthPermissionNames.ReadProgress, permissions);
    }

    [Fact]
    public async Task RefreshToken_WhenAdminRoleIsInactive_ShouldNotReturnAdminRoleClaim()
    {
        using var factory = new CourseCoreApiFactory();
        await factory.SetAdminRoleActiveAsync(false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        var login = await LoginAsync(client);

        var refresh = await RefreshAsync(client, login.RefreshToken);
        var roles = ReadRoleClaims(refresh.AccessToken);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.DoesNotContain(AuthRoleNames.Admin, roles);
    }

    [Fact]
    public async Task RefreshToken_WhenOldRefreshTokenIsReused_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();
        var login = await LoginAsync(client);
        var refresh = await RefreshAsync(client, login.RefreshToken);

        var reuseResponse = await client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken = login.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WhenSameTokenIsUsedConcurrently_ShouldAllowOnlyOneRotation()
    {
        using var factory = new CourseCoreApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        var login = await LoginAsync(client);

        var refreshAttempts = await Task.WhenAll(
            RefreshAsync(client, login.RefreshToken),
            RefreshAsync(client, login.RefreshToken));
        var statusCodes = refreshAttempts.Select(attempt => attempt.StatusCode).ToArray();
        var activeRefreshTokens = await factory.CountActiveRefreshTokensByEmailAsync(CourseCoreApiFactory.AdminEmail);

        Assert.Contains(HttpStatusCode.OK, statusCodes);
        Assert.Contains(HttpStatusCode.Unauthorized, statusCodes);
        Assert.Equal(1, activeRefreshTokens);
        Assert.Single(refreshAttempts, attempt => !string.IsNullOrWhiteSpace(attempt.RefreshToken));
    }

    [Fact]
    public async Task Logout_WhenRefreshTokenIsValid_ShouldReturnNoContentAndRevokeToken()
    {
        using var client = CreateClient();
        var login = await LoginAsync(client);

        var logout = await LogoutAsync(client, login.RefreshToken);
        var refreshAfterLogout = await RefreshAsync(client, login.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenCalledTwiceWithSameRefreshToken_ShouldReturnNoContent()
    {
        using var client = CreateClient();
        var login = await LoginAsync(client);

        var firstLogout = await LogoutAsync(client, login.RefreshToken);
        var secondLogout = await LogoutAsync(client, login.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, firstLogout.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondLogout.StatusCode);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    private static async Task<AuthTokenResult> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CourseCoreApiFactory.AdminEmail,
            password = CourseCoreApiFactory.AdminPassword
        });

        return await ReadAuthTokenAsync(response);
    }

    private static async Task<AuthTokenResult> RefreshAsync(HttpClient client, string refreshToken)
    {
        var response = await client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken
        });

        return await ReadAuthTokenAsync(response);
    }

    private static async Task<HttpResponseMessage> LogoutAsync(HttpClient client, string refreshToken)
    {
        return await client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken
        });
    }

    private static async Task<AuthTokenResult> ReadAuthTokenAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new AuthTokenResult(response.StatusCode, string.Empty, string.Empty);
        }

        using var json = JsonDocument.Parse(content);
        var token = json.RootElement.GetProperty("token");

        return new AuthTokenResult(
            response.StatusCode,
            token.GetProperty("accessToken").GetString() ?? string.Empty,
            token.GetProperty("refreshToken").GetString() ?? string.Empty);
    }

    private static IReadOnlyCollection<string> ReadPermissionClaims(string accessToken)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

        return jwt.Claims
            .Where(claim => claim.Type == AuthClaimTypes.Permission)
            .Select(claim => claim.Value)
            .ToArray();
    }

    private static IReadOnlyCollection<string> ReadRoleClaims(string accessToken)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

        return jwt.Claims
            .Where(claim => claim.Type is AuthClaimTypes.Role or "role")
            .Select(claim => claim.Value)
            .ToArray();
    }

    private sealed record AuthTokenResult(
        HttpStatusCode StatusCode,
        string AccessToken,
        string RefreshToken);
}
