using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

    private sealed record AuthTokenResult(
        HttpStatusCode StatusCode,
        string AccessToken,
        string RefreshToken);
}
