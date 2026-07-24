using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CourseCore.Api.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CourseCore.Api.Tests.Integration.Observability;

public class CorrelationIdIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private const string CorrelationHeader = "X-Correlation-ID";
    private readonly CourseCoreApiFactory _factory;

    public CorrelationIdIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealthLive_WhenCorrelationIdIsMissing_ShouldReturnGeneratedCorrelationIdHeader()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/health/live");
        var correlationId = GetCorrelationId(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task GetHealthLive_WhenCorrelationIdIsProvided_ShouldPreserveCorrelationIdHeader()
    {
        using var client = CreateClient();
        var correlationId = Guid.NewGuid().ToString("D");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add(CorrelationHeader, correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(correlationId, GetCorrelationId(response));
    }

    [Fact]
    public async Task GetMissingCourse_WhenApplicationErrorOccurs_ShouldIncludeCorrelationIdInBody()
    {
        using var client = CreateClient();
        var login = await LoginAsync(client);
        var correlationId = Guid.NewGuid().ToString("D");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        client.DefaultRequestHeaders.Add(CorrelationHeader, correlationId);

        var response = await client.GetAsync($"/api/courses/{Guid.NewGuid()}");
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(correlationId, GetCorrelationId(response));
        Assert.Equal(correlationId, json.RootElement.GetProperty("correlationId").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task AuthEndpoints_WhenCorrelationMiddlewareIsEnabled_ShouldContinueLoginAndRefresh()
    {
        using var client = CreateClient();

        var login = await LoginAsync(client);
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh-token", new
        {
            refreshToken = login.RefreshToken
        });
        var refresh = await ReadAuthTokenAsync(refreshResponse);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(login.RefreshToken));
        Assert.False(string.IsNullOrWhiteSpace(refresh.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refresh.RefreshToken));
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    private static string GetCorrelationId(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues(CorrelationHeader, out var values));

        return Assert.Single(values);
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
