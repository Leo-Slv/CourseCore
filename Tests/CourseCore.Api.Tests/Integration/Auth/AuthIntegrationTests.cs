using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
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
    private const string RefreshTokenCookieName = "coursecore_refresh_token";
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
    public async Task Login_WhenAdminCredentialsAreValid_ShouldReturnAccessTokenAndSetRefreshTokenCookie()
    {
        using var client = CreateClient();

        var login = await LoginAsync(client);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(login.RefreshToken));
        Assert.False(login.HasRefreshTokenInBody);
        Assert.Contains("httponly", login.RefreshCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", login.RefreshCookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WhenProduction_ShouldSetSecureCookieAndNotReturnRefreshTokenInBody()
    {
        SetProductionEnvironmentVariables();
        try
        {
            using var factory = CourseCoreApiFactory.Create("Production", new Dictionary<string, string?>
            {
                ["Auth:ExposeRefreshTokenInBody"] = "true"
            });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

            var login = await LoginAsync(client);

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            Assert.False(login.HasRefreshTokenInBody);
            Assert.Contains("secure", login.RefreshCookieHeader, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("httponly", login.RefreshCookieHeader, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            ClearProductionEnvironmentVariables();
        }
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
    public async Task Login_WhenAdminCredentialsAreValid_ShouldReturnTokenVersionClaim()
    {
        using var client = CreateClient();

        var login = await LoginAsync(client);
        var tokenVersion = ReadTokenVersionClaim(login.AccessToken);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(0, tokenVersion);
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
    public async Task Login_WhenUserIsInactive_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();
        var user = await _factory.SeedUserWithRoleAsync(
            AuthPermissionNames.ManageUsers,
            userActive: false);

        var login = await IntegrationAuth.LoginAsync(client, user);

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
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

        var refresh = await RefreshWithCookieAsync(client, login.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(refresh.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refresh.RefreshToken));
        Assert.NotEqual(login.RefreshToken, refresh.RefreshToken);
        Assert.False(refresh.HasRefreshTokenInBody);
        Assert.Contains(RefreshTokenCookieName, refresh.RefreshCookieHeader);
    }

    [Fact]
    public async Task RefreshToken_WhenRefreshTokenIsValid_ShouldReturnPermissionClaims()
    {
        using var client = CreateClient();
        var login = await LoginAsync(client);

        var refresh = await RefreshWithCookieAsync(client, login.RefreshToken);
        var permissions = ReadPermissionClaims(refresh.AccessToken);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.Contains(AuthPermissionNames.ManageUsers, permissions);
        Assert.Contains(AuthPermissionNames.ManageCourses, permissions);
        Assert.Contains(AuthPermissionNames.ReadProgress, permissions);
    }

    [Fact]
    public async Task RefreshToken_WhenRefreshTokenIsValid_ShouldReturnTokenVersionClaim()
    {
        using var client = CreateClient();
        var login = await LoginAsync(client);

        var refresh = await RefreshWithCookieAsync(client, login.RefreshToken);
        var tokenVersion = ReadTokenVersionClaim(refresh.AccessToken);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.Equal(0, tokenVersion);
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

        var refresh = await RefreshWithCookieAsync(client, login.RefreshToken);
        var roles = ReadRoleClaims(refresh.AccessToken);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.DoesNotContain(AuthRoleNames.Admin, roles);
    }

    [Fact]
    public async Task RefreshToken_WhenOldRefreshTokenIsReused_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();
        var login = await LoginAsync(client);
        var refresh = await RefreshWithCookieAsync(client, login.RefreshToken);

        using var reuseClient = CreateClient();
        var reuseResponse = await PostWithRefreshCookieAsync(reuseClient, "/api/auth/refresh-token", login.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WhenBodyFallbackIsEnabled_ShouldReturnNewTokens()
    {
        using var client = CreateClient();
        var login = await LoginAsync(client);

        var refresh = await RefreshAsync(client, login.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(refresh.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refresh.RefreshToken));
    }

    [Fact]
    public async Task RefreshToken_WhenBodyFallbackIsDisabled_ShouldReturnUnauthorizedWithoutCookie()
    {
        using var factory = CourseCoreApiFactory.Create("Development", new Dictionary<string, string?>
        {
            ["Auth:AllowRefreshTokenInBodyFallback"] = "false"
        });
        using var loginClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        var login = await LoginAsync(loginClient);
        using var refreshClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        var refresh = await RefreshAsync(refreshClient, login.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Login_WhenInvalidCredentialsExceedRateLimit_ShouldReturnTooManyRequests()
    {
        using var factory = CreateRateLimitedFactory(loginLimit: 2);
        using var client = CreateClient(factory);

        var first = await PostInvalidLoginAsync(client);
        var second = await PostInvalidLoginAsync(client);
        var third = await PostInvalidLoginAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.True(third.Headers.RetryAfter?.Delta is not null || third.Headers.RetryAfter?.Date is not null);
    }

    [Fact]
    public async Task RefreshToken_WhenInvalidRequestsExceedRateLimit_ShouldReturnTooManyRequests()
    {
        using var factory = CreateRateLimitedFactory(refreshLimit: 2);
        using var client = CreateClient(factory);

        var first = await RefreshAsync(client, "invalid-refresh-token");
        var second = await RefreshAsync(client, "invalid-refresh-token");
        var third = await RefreshAsync(client, "invalid-refresh-token");

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenRequestsExceedRateLimit_ShouldReturnTooManyRequests()
    {
        using var factory = CreateRateLimitedFactory(logoutLimit: 2);
        using var client = CreateClient(factory);

        var first = await LogoutAsync(client, "missing-refresh-token");
        var second = await LogoutAsync(client, "missing-refresh-token");
        var third = await LogoutAsync(client, "missing-refresh-token");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
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
            RefreshWithCookieAsync(client, login.RefreshToken),
            RefreshWithCookieAsync(client, login.RefreshToken));
        var statusCodes = refreshAttempts.Select(attempt => attempt.StatusCode).ToArray();
        var activeRefreshTokens = await factory.CountActiveRefreshTokensByEmailAsync(CourseCoreApiFactory.AdminEmail);

        Assert.Contains(HttpStatusCode.OK, statusCodes);
        Assert.Contains(HttpStatusCode.Unauthorized, statusCodes);
        Assert.Equal(1, activeRefreshTokens);
        Assert.Single(refreshAttempts, attempt => !string.IsNullOrWhiteSpace(attempt.RefreshToken));
    }

    [Fact]
    public async Task GetUsers_WhenTokenVersionIsOld_ShouldReturnUnauthorized()
    {
        using var factory = new CourseCoreApiFactory();
        var user = await factory.SeedUserAsync(AuthPermissionNames.ManageUsers);
        using var client = CreateClient(factory);
        var login = await IntegrationAuth.LoginAsync(client, user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        await factory.IncrementUserTokenVersionAsync(user.Id);
        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WhenUserIsDeactivatedAfterLogin_ShouldReturnUnauthorized()
    {
        using var factory = new CourseCoreApiFactory();
        var user = await factory.SeedUserAsync(AuthPermissionNames.ManageUsers);
        using var userClient = CreateClient(factory);
        var login = await IntegrationAuth.LoginAsync(userClient, user);
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        using var adminClient = CreateClient(factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);

        var update = await adminClient.PutAsJsonAsync($"/api/users/{user.Id}", new
        {
            name = "Deactivated Integration User",
            email = user.Email,
            active = false
        });
        var response = await userClient.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WhenUserIsDeactivated_ShouldReturnUnauthorizedAndRevokeRefreshTokens()
    {
        using var factory = new CourseCoreApiFactory();
        var user = await factory.SeedUserAsync(AuthPermissionNames.ManageUsers);
        using var userClient = CreateClient(factory);
        var login = await IntegrationAuth.LoginAsync(userClient, user);
        using var adminClient = CreateClient(factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);

        var update = await adminClient.PutAsJsonAsync($"/api/users/{user.Id}", new
        {
            name = "Deactivated Integration User",
            email = user.Email,
            active = false
        });
        using var refreshClient = CreateClient(factory);
        var refresh = await PostWithRefreshCookieAsync(refreshClient, "/api/auth/refresh-token", login.RefreshToken);
        var tokenVersion = await factory.GetUserTokenVersionAsync(user.Id);
        var activeRefreshTokens = await factory.CountActiveRefreshTokensByUserIdAsync(user.Id);

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.Equal(1, tokenVersion);
        Assert.Equal(0, activeRefreshTokens);
    }

    [Fact]
    public async Task Logout_WhenRefreshTokenIsValid_ShouldReturnNoContentAndRevokeToken()
    {
        using var client = CreateClient();
        var login = await LoginAsync(client);

        var logout = await LogoutWithCookieAsync(client, login.RefreshToken);
        var refreshAfterLogout = await RefreshWithCookieAsync(client, login.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains($"{RefreshTokenCookieName}=", GetSetCookieHeader(logout));
        Assert.Contains("expires=", GetSetCookieHeader(logout), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenCalledTwiceWithSameRefreshToken_ShouldReturnNoContent()
    {
        using var client = CreateClient();
        var login = await LoginAsync(client);

        var firstLogout = await LogoutWithCookieAsync(client, login.RefreshToken);
        var secondLogout = await LogoutWithCookieAsync(client, login.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, firstLogout.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondLogout.StatusCode);
    }

    private HttpClient CreateClient()
    {
        return CreateClient(_factory);
    }

    private static HttpClient CreateClient(CourseCoreApiFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    private static CourseCoreApiFactory CreateRateLimitedFactory(
        int loginLimit = 100,
        int refreshLimit = 100,
        int logoutLimit = 100)
    {
        return CourseCoreApiFactory.Create("Development", new Dictionary<string, string?>
        {
            ["RateLimiting:Login:PermitLimit"] = loginLimit.ToString(),
            ["RateLimiting:Login:WindowSeconds"] = "60",
            ["RateLimiting:Login:QueueLimit"] = "0",
            ["RateLimiting:Refresh:PermitLimit"] = refreshLimit.ToString(),
            ["RateLimiting:Refresh:WindowSeconds"] = "60",
            ["RateLimiting:Refresh:QueueLimit"] = "0",
            ["RateLimiting:Logout:PermitLimit"] = logoutLimit.ToString(),
            ["RateLimiting:Logout:WindowSeconds"] = "60",
            ["RateLimiting:Logout:QueueLimit"] = "0"
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

    private static Task<HttpResponseMessage> PostInvalidLoginAsync(HttpClient client)
    {
        return client.PostAsJsonAsync("/api/auth/login", new
        {
            email = $"missing-{Guid.NewGuid():N}@coursecore.local",
            password = "WrongPassword123!"
        });
    }

    private static async Task<AuthTokenResult> RefreshAsync(HttpClient client, string refreshToken)
    {
        var response = await client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken
        });

        return await ReadAuthTokenAsync(response);
    }

    private static async Task<AuthTokenResult> RefreshWithCookieAsync(HttpClient client, string refreshToken)
    {
        var response = await PostWithRefreshCookieAsync(client, "/api/auth/refresh-token", refreshToken);

        return await ReadAuthTokenAsync(response);
    }

    private static async Task<HttpResponseMessage> LogoutWithCookieAsync(HttpClient client, string refreshToken)
    {
        return await PostWithRefreshCookieAsync(client, "/api/auth/logout", refreshToken);
    }

    private static async Task<HttpResponseMessage> PostWithRefreshCookieAsync(
        HttpClient client,
        string path,
        string refreshToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new { refreshToken = string.Empty })
        };
        request.Headers.Add("Cookie", $"{RefreshTokenCookieName}={refreshToken}");

        return await client.SendAsync(request);
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
        var refreshCookieHeader = GetSetCookieHeader(response);
        var refreshTokenFromCookie = ReadRefreshTokenFromSetCookie(refreshCookieHeader);

        if (!response.IsSuccessStatusCode)
        {
            return new AuthTokenResult(
                response.StatusCode,
                string.Empty,
                refreshTokenFromCookie ?? string.Empty,
                refreshCookieHeader,
                HasRefreshTokenInBody: false);
        }

        using var json = JsonDocument.Parse(content);
        var token = json.RootElement.GetProperty("token");
        var hasRefreshTokenInBody = token.TryGetProperty("refreshToken", out var refreshTokenElement);
        var refreshToken = hasRefreshTokenInBody
            ? refreshTokenElement.GetString() ?? string.Empty
            : refreshTokenFromCookie ?? string.Empty;

        return new AuthTokenResult(
            response.StatusCode,
            token.GetProperty("accessToken").GetString() ?? string.Empty,
            refreshToken,
            refreshCookieHeader,
            hasRefreshTokenInBody);
    }

    private static string GetSetCookieHeader(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(value => value.StartsWith($"{RefreshTokenCookieName}=", StringComparison.OrdinalIgnoreCase)) ?? string.Empty
            : string.Empty;
    }

    private static string? ReadRefreshTokenFromSetCookie(string setCookieHeader)
    {
        if (string.IsNullOrWhiteSpace(setCookieHeader))
        {
            return null;
        }

        var prefix = $"{RefreshTokenCookieName}=";

        if (!setCookieHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var endIndex = setCookieHeader.IndexOf(';');

        return endIndex < 0
            ? setCookieHeader[prefix.Length..]
            : setCookieHeader[prefix.Length..endIndex];
    }

    private static void SetProductionEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__CourseCoreDatabase", "Data Source=:memory:");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "CourseCore.IntegrationTests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "CourseCore.IntegrationTests");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "integration-test-secret-key-32-characters-minimum");
        Environment.SetEnvironmentVariable("Media__Playback__SigningSecret", "integration-test-media-signing-secret-32-characters-minimum");
        Environment.SetEnvironmentVariable("Media__Playback__BaseUrl", "/media");
        Environment.SetEnvironmentVariable("Media__Playback__SignedUrlExpirationMinutes", "10");
        Environment.SetEnvironmentVariable("Media__Playback__AllowedStorageProviders__0", "Local");
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "https://localhost");
    }

    private static void ClearProductionEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__CourseCoreDatabase", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
        Environment.SetEnvironmentVariable("Jwt__SecretKey", null);
        Environment.SetEnvironmentVariable("Media__Playback__SigningSecret", null);
        Environment.SetEnvironmentVariable("Media__Playback__BaseUrl", null);
        Environment.SetEnvironmentVariable("Media__Playback__SignedUrlExpirationMinutes", null);
        Environment.SetEnvironmentVariable("Media__Playback__AllowedStorageProviders__0", null);
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", null);
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

    private static int ReadTokenVersionClaim(string accessToken)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var value = jwt.Claims.Single(claim => claim.Type == AuthClaimTypes.TokenVersion).Value;

        return int.Parse(value, CultureInfo.InvariantCulture);
    }

    private sealed record AuthTokenResult(
        HttpStatusCode StatusCode,
        string AccessToken,
        string RefreshToken,
        string RefreshCookieHeader,
        bool HasRefreshTokenInBody);
}
