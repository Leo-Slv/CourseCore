using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Access;

public class AreaManagementIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public AreaManagementIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateArea_WhenAdminPostsValidRequest_ShouldReturnCreatedWithLocation()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/areas", CreateAreaRequest());
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.True(json.RootElement.GetProperty("id").GetGuid() != Guid.Empty);
        Assert.True(json.RootElement.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task CreateArea_WhenUserHasManageAreasPermission_ShouldReturnCreated()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync(AuthPermissionNames.ManageAreas);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/areas", CreateAreaRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateArea_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync("/api/areas", CreateAreaRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateArea_WhenUserHasNoPermissionOrAdminRole_ShouldReturnForbidden()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/areas", CreateAreaRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateArea_WhenNameIsEmpty_ShouldReturnBadRequest()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/areas", new
        {
            name = string.Empty,
            slug = $"area-{Guid.NewGuid():N}",
            description = "Description",
            displayOrder = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateArea_WhenSlugIsInvalid_ShouldReturnBadRequest()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/areas", new
        {
            name = "Invalid Slug Area",
            slug = "Not A Slug",
            description = "Description",
            displayOrder = 0
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateArea_WhenSlugAlreadyExists_ShouldReturnConflict()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var request = CreateAreaRequest();
        await client.PostAsJsonAsync("/api/areas", request);

        var response = await client.PostAsJsonAsync("/api/areas", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetAreaById_WhenAreaExists_ShouldReturnOk()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var areaId = await CreateAreaAsync(client);

        var response = await client.GetAsync($"/api/areas/{areaId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAreaById_WhenAreaDoesNotExist_ShouldReturnNotFound()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync($"/api/areas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListAreas_WhenNoFilterIsProvided_ShouldReturnCreatedArea()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var areaId = await CreateAreaAsync(client);

        var json = await client.GetFromJsonAsync<JsonElement>("/api/areas");

        Assert.Equal(JsonValueKind.Array, json.ValueKind);
        Assert.Contains(json.EnumerateArray(), item => item.GetProperty("id").GetGuid() == areaId);
    }

    [Fact]
    public async Task ListAreas_WhenActiveFilterIsNotABoolean_ShouldReturnBadRequest()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync("/api/areas?active=not-a-boolean");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateArea_WhenAdminPutsValidRequest_ShouldReturnOk()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var areaId = await CreateAreaAsync(client);

        var response = await client.PutAsJsonAsync($"/api/areas/{areaId}", new
        {
            name = "Updated Area",
            slug = $"updated-area-{Guid.NewGuid():N}",
            description = "Updated description",
            displayOrder = 20,
            active = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateArea_WhenDeactivating_ShouldReturnInactiveArea()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var areaId = await CreateAreaAsync(client);

        var response = await client.PutAsJsonAsync($"/api/areas/{areaId}", new
        {
            name = "Area",
            slug = $"area-{areaId:N}",
            description = "Description",
            displayOrder = 0,
            active = false
        });
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(json.RootElement.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task UpdateArea_WhenAreaDoesNotExist_ShouldReturnNotFound()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PutAsJsonAsync($"/api/areas/{Guid.NewGuid()}", new
        {
            name = "Missing Area",
            slug = $"missing-area-{Guid.NewGuid():N}",
            description = "Description",
            displayOrder = 0,
            active = true
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateArea_WhenSlugConflictsWithAnotherArea_ShouldReturnConflict()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var firstAreaSlug = $"first-area-{Guid.NewGuid():N}";
        var secondAreaSlug = $"second-area-{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/areas", new
        {
            name = "First Area",
            slug = firstAreaSlug,
            description = "Description",
            displayOrder = 0
        });
        var secondAreaResponse = await client.PostAsJsonAsync("/api/areas", new
        {
            name = "Second Area",
            slug = secondAreaSlug,
            description = "Description",
            displayOrder = 0
        });
        var secondAreaContent = await secondAreaResponse.Content.ReadAsStringAsync();
        using var secondAreaJson = JsonDocument.Parse(secondAreaContent);
        var secondAreaId = secondAreaJson.RootElement.GetProperty("id").GetGuid();

        var response = await client.PutAsJsonAsync($"/api/areas/{secondAreaId}", new
        {
            name = "Second Area",
            slug = firstAreaSlug,
            description = "Description",
            displayOrder = 0,
            active = true
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task<Guid> CreateAreaAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/areas", CreateAreaRequest());
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        return json.RootElement.GetProperty("id").GetGuid();
    }

    private static CreateAreaRequestBody CreateAreaRequest()
    {
        return new CreateAreaRequestBody($"area-{Guid.NewGuid():N}");
    }

    private sealed record CreateAreaRequestBody(string Slug)
    {
        public string Name { get; } = "Integration Area";

        public string Description { get; } = "Access to integration content";

        public int DisplayOrder { get; } = 0;
    }
}
