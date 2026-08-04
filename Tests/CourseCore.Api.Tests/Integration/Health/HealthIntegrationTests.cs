using System.Net;
using System.Text.Json;
using CourseCore.Api.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CourseCore.Api.Tests.Integration.Health;

[Collection("Production environment")]
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
        Assert.False(json.RootElement.TryGetProperty("entries", out _));
        Assert.False(json.RootElement.TryGetProperty("totalDuration", out _));
    }

    [Fact]
    public async Task GetHealthEndpoints_WhenProduction_ShouldNotExposeOperationalDetails()
    {
        SetProductionEnvironmentVariables();
        try
        {
            await using var factory = CourseCoreApiFactory.Create("Production", new Dictionary<string, string?>
            {
                ["Auth:RefreshTokenCookie:Secure"] = "true"
            });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

            foreach (var path in new[] { "/health/ready", "/health" })
            {
                var response = await client.GetAsync(path);
                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
                Assert.False(json.RootElement.TryGetProperty("entries", out _));
                Assert.False(json.RootElement.TryGetProperty("totalDuration", out _));
            }
        }
        finally
        {
            ClearProductionEnvironmentVariables();
        }
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
        Environment.SetEnvironmentVariable("Auth__RefreshTokenCookie__Secure", "true");
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
        Environment.SetEnvironmentVariable("Auth__RefreshTokenCookie__Secure", null);
    }
}

[CollectionDefinition("Production environment", DisableParallelization = true)]
public sealed class ProductionEnvironmentCollection;
