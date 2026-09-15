using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Modules.Media.Presentation.Responses;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Media;

public class ImageUploadIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public ImageUploadIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RequestCourseThumbnailUploadUrl_WhenAdminPostsValidRequest_ShouldReturnOk()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/courses/{course.CourseId}/thumbnail-upload-url",
            UploadRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UploadUrlResponse>();
        Assert.NotNull(body);
        Assert.Equal("S3", body!.StorageProvider);
        Assert.StartsWith($"course-thumbnails/{course.CourseId:N}/", body.StorageKey);
        Assert.False(string.IsNullOrWhiteSpace(body.UploadUrl));
    }

    [Fact]
    public async Task RequestCourseThumbnailUploadUrl_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync(
            $"/api/courses/{Guid.NewGuid()}/thumbnail-upload-url",
            UploadRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RequestCourseThumbnailUploadUrl_WhenUserHasNoPermissionOrAdminRole_ShouldReturnForbidden()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync(
            $"/api/courses/{Guid.NewGuid()}/thumbnail-upload-url",
            UploadRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RequestCourseThumbnailUploadUrl_WhenCourseDoesNotExist_ShouldReturnNotFound()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/courses/{Guid.NewGuid()}/thumbnail-upload-url",
            UploadRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RequestCourseModuleImageUploadUrl_WhenAdminPostsValidRequest_ShouldReturnOk()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/courses/{course.CourseId}/modules/{course.ModuleId}/image-upload-url",
            UploadRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UploadUrlResponse>();
        Assert.NotNull(body);
        Assert.StartsWith($"module-covers/{course.ModuleId:N}/", body!.StorageKey);
    }

    [Fact]
    public async Task RequestCourseModuleImageUploadUrl_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync(
            $"/api/courses/{Guid.NewGuid()}/modules/{Guid.NewGuid()}/image-upload-url",
            UploadRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RequestCourseModuleImageUploadUrl_WhenModuleDoesNotExist_ShouldReturnNotFound()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/courses/{course.CourseId}/modules/{Guid.NewGuid()}/image-upload-url",
            UploadRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RequestAreaImageUploadUrl_WhenAdminPostsValidRequest_ShouldReturnOk()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/areas/{course.AreaId}/image-upload-url",
            UploadRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UploadUrlResponse>();
        Assert.NotNull(body);
        Assert.StartsWith($"area-covers/{course.AreaId:N}/", body!.StorageKey);
    }

    [Fact]
    public async Task RequestAreaImageUploadUrl_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync(
            $"/api/areas/{Guid.NewGuid()}/image-upload-url",
            UploadRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RequestAreaImageUploadUrl_WhenAreaDoesNotExist_ShouldReturnNotFound()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/api/areas/{Guid.NewGuid()}/image-upload-url",
            UploadRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RequestAvatarUploadUrl_WhenAuthenticatedUserPostsValidRequest_ShouldReturnOk()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/auth/me/avatar-upload-url", UploadRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UploadUrlResponse>();
        Assert.NotNull(body);
        Assert.StartsWith($"avatars/{user.Id:N}/", body!.StorageKey);
    }

    [Fact]
    public async Task RequestAvatarUploadUrl_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/me/avatar-upload-url", UploadRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static object UploadRequest()
    {
        return new
        {
            fileName = "image.png",
            contentType = "image/png",
            sizeBytes = 1024
        };
    }
}
