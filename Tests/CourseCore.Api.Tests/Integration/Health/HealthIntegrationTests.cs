using System.Net;
using System.Text.Json;
using CourseCore.Api.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CourseCore.Api.Tests.Integration.Health;

public class HealthIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public HealthIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealthLive_WhenAnonymous_ShouldReturnOk()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/health/live");
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
        Assert.True(json.RootElement.TryGetProperty("entries", out _));
    }

    [Fact]
    public async Task GetHealthReady_WhenDatabaseIsHealthy_ShouldReturnOk()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/health/ready");
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
        Assert.True(json.RootElement.GetProperty("entries").TryGetProperty("database", out _));
    }

    [Fact]
    public async Task GetHealth_WhenDatabaseIsHealthy_ShouldReturnOk()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/health");
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
        Assert.True(json.RootElement.TryGetProperty("entries", out _));
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }
}
