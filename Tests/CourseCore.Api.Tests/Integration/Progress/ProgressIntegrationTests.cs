using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Modules.Progress.Presentation.Responses;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Progress;

public class ProgressIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public ProgressIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RegisterLessonProgress_WhenUserHasCourseAccess_ShouldReturnOk()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        await _factory.SeedReadyVideoAsync(course.LessonId);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/progress/lessons", new
        {
            lessonId = course.LessonId,
            watchedSeconds = 90,
            markAsCompleted = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RegisterLessonProgress_WhenClientMarksCompletedWithZeroSeconds_ShouldNotCompleteLesson()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        await _factory.SeedReadyVideoAsync(course.LessonId, durationSeconds: 100);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/progress/lessons", new
        {
            lessonId = course.LessonId,
            watchedSeconds = 0,
            markAsCompleted = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LessonProgressResponse>();
        Assert.NotNull(body);
        Assert.False(body.Completed);
        Assert.Equal(0, body.WatchedSeconds);
    }

    [Fact]
    public async Task RegisterLessonProgress_WhenWatchedSecondsReachThreshold_ShouldCompleteLesson()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        await _factory.SeedReadyVideoAsync(course.LessonId, durationSeconds: 100);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/progress/lessons", new
        {
            lessonId = course.LessonId,
            watchedSeconds = 90,
            markAsCompleted = false
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LessonProgressResponse>();
        Assert.NotNull(body);
        Assert.True(body.Completed);
        Assert.Equal(90, body.WatchedSeconds);

        var courseProgress = await _factory.GetCourseProgressAsync(user.Id, course.CourseId);
        Assert.NotNull(courseProgress);
        Assert.Equal(100, courseProgress.ProgressPercent);
        Assert.NotNull(courseProgress.CompletedAt);
    }

    [Fact]
    public async Task RegisterLessonProgress_WhenWatchedSecondsExceedDuration_ShouldClampPersistedProgress()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        await _factory.SeedReadyVideoAsync(course.LessonId, durationSeconds: 100);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/progress/lessons", new
        {
            lessonId = course.LessonId,
            watchedSeconds = 999,
            markAsCompleted = true
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LessonProgressResponse>();
        Assert.NotNull(body);
        Assert.Equal(100, body.WatchedSeconds);

        var persisted = await _factory.GetLessonProgressAsync(user.Id, course.LessonId);
        Assert.NotNull(persisted);
        Assert.Equal(100, persisted.WatchedSeconds);
    }

    [Fact]
    public async Task RegisterLessonProgress_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync("/api/progress/lessons", new
        {
            lessonId = Guid.NewGuid(),
            watchedSeconds = 90,
            markAsCompleted = true
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisterLessonProgress_WhenUserHasNoCourseAccess_ShouldReturnForbidden()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/progress/lessons", new
        {
            lessonId = course.LessonId,
            watchedSeconds = 90,
            markAsCompleted = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RegisterLessonProgress_WhenLessonDoesNotExist_ShouldReturnNotFound()
    {
        var user = await _factory.SeedUserAsync();
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/progress/lessons", new
        {
            lessonId = Guid.NewGuid(),
            watchedSeconds = 90,
            markAsCompleted = true
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCourseProgress_WhenUserHasCourseAccess_ShouldReturnOk()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync($"/api/progress/courses/{course.CourseId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl!.NoStore);
    }

    [Fact]
    public async Task GetCourseProgress_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.GetAsync($"/api/progress/courses/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCourseProgress_WhenCourseDoesNotExist_ShouldReturnNotFound()
    {
        var user = await _factory.SeedUserAsync();
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync($"/api/progress/courses/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCourseProgress_WhenCalledAsLegacyPost_ShouldReturnNotFound()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/progress/courses", new
        {
            courseId = course.CourseId
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
