using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Modules.Media.Presentation.Responses;
using CourseCore.Api.Modules.Media.Application.Validation;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Media;

public class VideosIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public VideosIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateVideo_WhenAdminPostsValidRequest_ShouldReturnCreated()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/videos", CreateVideoRequest(course.LessonId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<VideoResponse>();
        Assert.NotNull(body);
        Assert.Equal("Processing", body.Status);
        Assert.Null(body.PlaybackUrl);
    }

    [Fact]
    public async Task CreateVideo_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync("/api/videos", CreateVideoRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateVideo_WhenUserHasNoPermissionOrAdminRole_ShouldReturnForbidden()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/videos", CreateVideoRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/image.png")]
    public async Task CreateVideo_WhenThumbnailUrlIsInvalid_ShouldReturnBadRequest(string thumbnailUrl)
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/videos", new
        {
            lessonId = course.LessonId,
            title = "Video",
            description = "Description",
            storageProvider = "Local",
            storageKey = "videos/video.mp4",
            thumbnailUrl,
            durationSeconds = 120,
            sizeBytes = 1024
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateVideo_WhenTitleIsTooLong_ShouldReturnBadRequest()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/videos", new
        {
            lessonId = course.LessonId,
            title = new string('V', MediaValidationLimits.TitleMaxLength + 1),
            description = "Description",
            storageProvider = "Local",
            storageKey = "videos/video.mp4",
            durationSeconds = 120,
            sizeBytes = 1024
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RequestPlayback_WhenUserHasCourseAccess_ShouldReturnOk()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        var video = await _factory.SeedReadyVideoAsync(course.LessonId);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync($"/api/videos/{video.VideoId}/playback");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl!.NoStore);
        var body = await response.Content.ReadFromJsonAsync<VideoPlaybackResponse>();
        Assert.NotNull(body);
        Assert.Contains($"/videos/{video.VideoId}/playback", body.PlaybackUrl);
        Assert.Contains("expires=", body.PlaybackUrl);
        Assert.Contains("signature=", body.PlaybackUrl);
        Assert.True(body.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateVideo_WhenPlaybackUrlIsArbitrary_ShouldNotUseItAsPlayback()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/videos", CreateVideoRequest(course.LessonId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<VideoResponse>();
        Assert.NotNull(body);
        Assert.Equal("Processing", body.Status);
        Assert.Null(body.PlaybackUrl);
    }

    [Fact]
    public async Task MarkReady_WhenAdminPostsExistingVideo_ShouldMarkVideoReadyWithoutPlaybackUrl()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var create = await client.PostAsJsonAsync("/api/videos", CreateVideoRequest(course.LessonId));
        var created = await create.Content.ReadFromJsonAsync<VideoResponse>();
        Assert.NotNull(created);

        var response = await client.PostAsync($"/api/videos/{created.Id}/ready", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<VideoResponse>();
        Assert.NotNull(body);
        Assert.Equal("Ready", body.Status);
        Assert.Null(body.PlaybackUrl);
    }

    [Fact]
    public async Task RequestPlayback_WhenVideoIsNotReady_ShouldReturnConflict()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var adminClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);
        var create = await adminClient.PostAsJsonAsync("/api/videos", CreateVideoRequest(course.LessonId));
        var created = await create.Content.ReadFromJsonAsync<VideoResponse>();
        Assert.NotNull(created);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync($"/api/videos/{created.Id}/playback");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RequestPlayback_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.GetAsync($"/api/videos/{Guid.NewGuid()}/playback");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RequestPlayback_WhenUserHasNoCourseAccess_ShouldReturnForbidden()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        var video = await _factory.SeedReadyVideoAsync(course.LessonId);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync($"/api/videos/{video.VideoId}/playback");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RequestPlayback_WhenVideoDoesNotExist_ShouldReturnNotFound()
    {
        var user = await _factory.SeedUserAsync();
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync($"/api/videos/{Guid.NewGuid()}/playback");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RequestPlayback_WhenCalledAsLegacyPost_ShouldReturnNotFound()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        var video = await _factory.SeedReadyVideoAsync(course.LessonId);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/videos/playback", new
        {
            videoId = video.VideoId
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static object CreateVideoRequest(Guid lessonId)
    {
        return new
        {
            lessonId,
            title = "Created Integration Video",
            description = "Created integration video",
            storageProvider = "Local",
            storageKey = $"videos/{Guid.NewGuid():N}.mp4",
            playbackUrl = "https://media.coursecore.local/created.mp4",
            thumbnailUrl = "https://media.coursecore.local/created.png",
            durationSeconds = 120,
            sizeBytes = 2048
        };
    }
}
