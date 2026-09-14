using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Modules.Media.Presentation.Responses;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Media;

public class LessonMaterialsIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public LessonMaterialsIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateMaterial_WhenAdminPostsValidRequest_ShouldReturnCreated()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/materials/lessons/{course.LessonId}",
            CreateMaterialRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LessonMaterialResponse>();
        Assert.NotNull(body);
        Assert.Equal(course.LessonId, body!.LessonId);
        Assert.Equal(0, body.DisplayOrder);
    }

    [Fact]
    public async Task CreateMaterial_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync(
            $"/api/materials/lessons/{Guid.NewGuid()}",
            CreateMaterialRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateMaterial_WhenUserHasNoPermissionOrAdminRole_ShouldReturnForbidden()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync(
            $"/api/materials/lessons/{Guid.NewGuid()}",
            CreateMaterialRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListMaterials_WhenLessonHasMultipleMaterials_ShouldReturnThemOrderedByDisplayOrder()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        await client.PostAsJsonAsync($"/api/materials/lessons/{course.LessonId}", CreateMaterialRequest("First"));
        await client.PostAsJsonAsync($"/api/materials/lessons/{course.LessonId}", CreateMaterialRequest("Second"));

        var response = await client.GetAsync($"/api/materials/lessons/{course.LessonId}");
        var body = await response.Content.ReadFromJsonAsync<List<LessonMaterialResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(["First", "Second"], body!.Select(material => material.Title));
    }

    [Fact]
    public async Task UpdateMaterial_WhenMaterialExists_ShouldReturnUpdatedMaterial()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var create = await client.PostAsJsonAsync($"/api/materials/lessons/{course.LessonId}", CreateMaterialRequest());
        var created = await create.Content.ReadFromJsonAsync<LessonMaterialResponse>();
        Assert.NotNull(created);

        var response = await client.PutAsJsonAsync($"/api/materials/{created!.Id}", new
        {
            title = "Updated title",
            displayOrder = 5
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LessonMaterialResponse>();
        Assert.NotNull(body);
        Assert.Equal("Updated title", body!.Title);
        Assert.Equal(5, body.DisplayOrder);
    }

    [Fact]
    public async Task RemoveMaterial_WhenMaterialExists_ShouldReturnNoContent()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var create = await client.PostAsJsonAsync($"/api/materials/lessons/{course.LessonId}", CreateMaterialRequest());
        var created = await create.Content.ReadFromJsonAsync<LessonMaterialResponse>();
        Assert.NotNull(created);

        var response = await client.DeleteAsync($"/api/materials/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var listResponse = await client.GetAsync($"/api/materials/lessons/{course.LessonId}");
        var body = await listResponse.Content.ReadFromJsonAsync<List<LessonMaterialResponse>>();
        Assert.NotNull(body);
        Assert.Empty(body!);
    }

    [Fact]
    public async Task ReorderMaterials_WhenItemsMatchExistingMaterials_ShouldReturnNoContentAndPersistOrder()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var firstCreate = await client.PostAsJsonAsync($"/api/materials/lessons/{course.LessonId}", CreateMaterialRequest("First"));
        var first = await firstCreate.Content.ReadFromJsonAsync<LessonMaterialResponse>();
        var secondCreate = await client.PostAsJsonAsync($"/api/materials/lessons/{course.LessonId}", CreateMaterialRequest("Second"));
        var second = await secondCreate.Content.ReadFromJsonAsync<LessonMaterialResponse>();
        Assert.NotNull(first);
        Assert.NotNull(second);

        var response = await client.PatchAsJsonAsync($"/api/materials/lessons/{course.LessonId}/order", new
        {
            items = new[]
            {
                new { materialId = first!.Id, displayOrder = 1 },
                new { materialId = second!.Id, displayOrder = 0 }
            }
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var listResponse = await client.GetAsync($"/api/materials/lessons/{course.LessonId}");
        var body = await listResponse.Content.ReadFromJsonAsync<List<LessonMaterialResponse>>();
        Assert.NotNull(body);
        Assert.Equal(["Second", "First"], body!.Select(material => material.Title));
    }

    [Fact]
    public async Task ListMaterials_WhenStudentHasCourseAccess_ShouldReturnOk()
    {
        using var adminClient = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);
        await adminClient.PostAsJsonAsync($"/api/materials/lessons/{course.LessonId}", CreateMaterialRequest());

        using var studentClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(studentClient, user);

        var response = await studentClient.GetAsync($"/api/materials/lessons/{course.LessonId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<LessonMaterialResponse>>();
        Assert.NotNull(body);
        Assert.Single(body!);
    }

    [Fact]
    public async Task ListMaterials_WhenStudentHasNoCourseAccess_ShouldReturnForbidden()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync($"/api/materials/lessons/{course.LessonId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListMaterials_WhenAdminHasNoCourseAccess_ShouldReturnOk()
    {
        using var adminClient = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);
        await adminClient.PostAsJsonAsync($"/api/materials/lessons/{course.LessonId}", CreateMaterialRequest());

        var response = await adminClient.GetAsync($"/api/materials/lessons/{course.LessonId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDownloadUrl_WhenStudentHasCourseAccess_ShouldReturnOk()
    {
        using var adminClient = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);
        var create = await adminClient.PostAsJsonAsync($"/api/materials/lessons/{course.LessonId}", CreateMaterialRequest());
        var created = await create.Content.ReadFromJsonAsync<LessonMaterialResponse>();
        Assert.NotNull(created);

        using var studentClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(studentClient, user);

        var response = await studentClient.GetAsync($"/api/materials/{created!.Id}/download");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MaterialDownloadResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.DownloadUrl));
    }

    [Fact]
    public async Task GetDownloadUrl_WhenStudentHasNoCourseAccess_ShouldReturnForbidden()
    {
        using var adminClient = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);
        var create = await adminClient.PostAsJsonAsync($"/api/materials/lessons/{course.LessonId}", CreateMaterialRequest());
        var created = await create.Content.ReadFromJsonAsync<LessonMaterialResponse>();
        Assert.NotNull(created);

        using var studentClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(studentClient, user);

        var response = await studentClient.GetAsync($"/api/materials/{created!.Id}/download");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static object CreateMaterialRequest(string title = "Apostila — Módulo 01")
    {
        return new
        {
            title,
            fileName = "modulo-01.pdf",
            contentType = "application/pdf",
            storageProvider = "Local",
            storageKey = $"materials/{Guid.NewGuid():N}.pdf",
            sizeBytes = 1_200_000
        };
    }
}
