using System.Net;
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
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Bearer", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetOpenApiJson_WhenDevelopmentEnvironment_ShouldContainBearerScheme()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("bearer", content, StringComparison.OrdinalIgnoreCase);
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
}
