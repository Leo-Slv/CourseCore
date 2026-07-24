using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CourseCore.Api.Tests.Integration.Infrastructure;

public static class IntegrationAuth
{
    public static HttpClient CreateClient(CourseCoreApiFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    public static async Task<AuthTokenResult> LoginAdminAsync(HttpClient client)
    {
        return await LoginAsync(client, CourseCoreApiFactory.AdminEmail, CourseCoreApiFactory.AdminPassword);
    }

    public static async Task<AuthTokenResult> LoginAsync(HttpClient client, TestUser user)
    {
        return await LoginAsync(client, user.Email, user.Password);
    }

    public static async Task<AuthTokenResult> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

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

    public static async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var login = await LoginAdminAsync(client);

        if (login.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException("Admin login failed.");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }

    public static async Task AuthenticateAsAsync(HttpClient client, TestUser user)
    {
        var login = await LoginAsync(client, user);

        if (login.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException("User login failed.");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }
}

public sealed record AuthTokenResult(
    HttpStatusCode StatusCode,
    string AccessToken,
    string RefreshToken);
