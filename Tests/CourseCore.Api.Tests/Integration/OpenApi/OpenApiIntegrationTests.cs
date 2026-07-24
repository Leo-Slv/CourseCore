using System.Net;
using System.Text.Json;
using CourseCore.Api.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CourseCore.Api.Tests.Integration.OpenApi;

public class OpenApiIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public OpenApiIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetOpenApiJson_WhenDevelopmentEnvironment_ShouldReturnOk()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOpenApiJson_WhenDevelopmentEnvironment_ShouldContainBearer()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.True(
            json.RootElement
                .GetProperty("components")
                .GetProperty("securitySchemes")
                .TryGetProperty("Bearer", out _));
    }

    [Fact]
    public async Task GetOpenApiJson_WhenDevelopmentEnvironment_ShouldContainBearerScheme()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var bearerScheme = json.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");

        Assert.Equal("http", bearerScheme.GetProperty("type").GetString());
        Assert.Equal("bearer", bearerScheme.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearerScheme.GetProperty("bearerFormat").GetString());
    }

    [Fact]
    public async Task GetOpenApiJson_WhenDevelopmentEnvironment_ShouldNotContainGlobalSecurityRequirement()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.False(json.RootElement.TryGetProperty("security", out _));
    }

    [Theory]
    [InlineData("/api/auth/login", "post")]
    [InlineData("/api/auth/refresh-token", "post")]
    public async Task GetOpenApiJson_WhenEndpointAllowsAnonymous_ShouldNotRequireBearer(
        string path,
        string method)
    {
        using var client = CreateClient();

        var operation = await GetOpenApiOperationAsync(client, path, method);

        Assert.False(operation.TryGetProperty("security", out _));
    }

    [Theory]
    [InlineData("/api/users", "get")]
    [InlineData("/api/courses/available", "get")]
    [InlineData("/api/videos/playback", "post")]
    [InlineData("/api/progress/lessons", "post")]
    public async Task GetOpenApiJson_WhenEndpointRequiresAuthorization_ShouldRequireBearer(
        string path,
        string method)
    {
        using var client = CreateClient();

        var operation = await GetOpenApiOperationAsync(client, path, method);

        Assert.True(OperationRequiresBearer(operation));
    }

    [Fact]
    public async Task GetScalar_WhenDevelopmentEnvironment_ShouldReturnOk()
    {
        using var client = CreateClient(allowAutoRedirect: true);

        var response = await client.GetAsync("/scalar");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType ?? string.Empty);
    }

    private HttpClient CreateClient(bool allowAutoRedirect = false)
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = allowAutoRedirect
        });
    }

    private static async Task<JsonElement> GetOpenApiOperationAsync(
        HttpClient client,
        string path,
        string method)
    {
        var response = await client.GetAsync("/openapi/v1.json");
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        return json.RootElement
            .GetProperty("paths")
            .GetProperty(path)
            .GetProperty(method)
            .Clone();
    }

    private static bool OperationRequiresBearer(JsonElement operation)
    {
        if (!operation.TryGetProperty("security", out var securityRequirements))
        {
            return false;
        }

        return securityRequirements.EnumerateArray().Any(requirement =>
            requirement.TryGetProperty("Bearer", out _));
    }
}
