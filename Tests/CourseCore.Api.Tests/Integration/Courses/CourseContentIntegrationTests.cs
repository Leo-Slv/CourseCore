using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Modules.Courses.Presentation.Responses;
using CourseCore.Api.Modules.Media.Presentation.Responses;
using CourseCore.Api.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CourseCore.Api.Tests.Integration.Courses;

public class CourseContentIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public CourseContentIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ModuleAndLessonLifecycle_WhenAdmin_ShouldRoundTrip()
    {
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var course = await _factory.SeedPublishedCourseWithoutContentAsync();

        var createModuleResponse = await client.PostAsJsonAsync(
            $"/api/courses/{course.CourseId}/modules",
            new { title = "Module 1", description = "First module" });
        await AssertStatusAsync(HttpStatusCode.Created, createModuleResponse);
        var module = await createModuleResponse.Content.ReadFromJsonAsync<CourseModuleResponse>();
        Assert.NotNull(module);
        Assert.Equal(0, module!.DisplayOrder);

        var updateModuleResponse = await client.PutAsJsonAsync(
            $"/api/courses/{course.CourseId}/modules/{module.Id}",
            new { title = "Module 1 Renamed", description = "Updated", published = true });
        await AssertStatusAsync(HttpStatusCode.OK, updateModuleResponse);
        var updatedModule = await updateModuleResponse.Content.ReadFromJsonAsync<CourseModuleResponse>();
        Assert.Equal("Module 1 Renamed", updatedModule!.Title);
        Assert.True(updatedModule.Published);

        var createLessonResponse = await client.PostAsJsonAsync(
            $"/api/courses/{course.CourseId}/modules/{module.Id}/lessons",
            new { title = "Lesson 1", description = "First lesson", freePreview = true });
        await AssertStatusAsync(HttpStatusCode.Created, createLessonResponse);
        var lesson = await createLessonResponse.Content.ReadFromJsonAsync<LessonResponse>();
        Assert.NotNull(lesson);
        Assert.True(lesson!.FreePreview);

        var secondLessonResponse = await client.PostAsJsonAsync(
            $"/api/courses/{course.CourseId}/modules/{module.Id}/lessons",
            new { title = "Lesson 2", description = "Second lesson", freePreview = false });
        await AssertStatusAsync(HttpStatusCode.Created, secondLessonResponse);
        var secondLesson = await secondLessonResponse.Content.ReadFromJsonAsync<LessonResponse>();

        var updateLessonResponse = await client.PutAsJsonAsync(
            $"/api/courses/{course.CourseId}/modules/{module.Id}/lessons/{lesson.Id}",
            new { title = "Lesson 1 Renamed", description = "Updated", freePreview = false, published = true });
        await AssertStatusAsync(HttpStatusCode.OK, updateLessonResponse);
        var updatedLesson = await updateLessonResponse.Content.ReadFromJsonAsync<LessonResponse>();
        Assert.Equal("Lesson 1 Renamed", updatedLesson!.Title);
        Assert.False(updatedLesson.FreePreview);

        var reorderLessonsResponse = await client.PutAsJsonAsync(
            $"/api/courses/{course.CourseId}/modules/{module.Id}/lessons/reorder",
            new { lessonIds = new[] { secondLesson!.Id, lesson.Id } });
        await AssertStatusAsync(HttpStatusCode.NoContent, reorderLessonsResponse);

        var removeLessonResponse = await client.DeleteAsync(
            $"/api/courses/{course.CourseId}/modules/{module.Id}/lessons/{lesson.Id}");
        await AssertStatusAsync(HttpStatusCode.NoContent, removeLessonResponse);

        var removeSecondLessonResponse = await client.DeleteAsync(
            $"/api/courses/{course.CourseId}/modules/{module.Id}/lessons/{secondLesson.Id}");
        await AssertStatusAsync(HttpStatusCode.NoContent, removeSecondLessonResponse);

        var removeModuleResponse = await client.DeleteAsync($"/api/courses/{course.CourseId}/modules/{module.Id}");
        await AssertStatusAsync(HttpStatusCode.NoContent, removeModuleResponse);
    }

    [Fact]
    public async Task ListCourseModules_WhenAdminHasNoPersonalCourseAccess_ShouldStillReturnModules()
    {
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        // grantUserAccess left null: the admin has no personal area/course access to this
        // locked paid course — this endpoint must not gate on the caller's own access.
        var course = await _factory.SeedPublishedCourseWithLessonAsync();

        var response = await client.GetAsync($"/api/courses/{course.CourseId}/modules");
        var body = await response.Content.ReadFromJsonAsync<List<CourseModuleResponse>>();

        await AssertStatusAsync(HttpStatusCode.OK, response);
        Assert.NotNull(body);
        Assert.Contains(body!, m => m.Id == course.ModuleId);
        var moduleResponse = body!.Single(m => m.Id == course.ModuleId);
        Assert.Contains(moduleResponse.Lessons, l => l.Id == course.LessonId);
    }

    [Fact]
    public async Task ListCourseModules_WhenCourseDoesNotExist_ShouldReturnNotFound()
    {
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.GetAsync($"/api/courses/{Guid.NewGuid()}/modules");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListCourseModules_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();
        var course = await _factory.SeedPublishedCourseWithoutContentAsync();

        var response = await client.GetAsync($"/api/courses/{course.CourseId}/modules");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RemoveCourseModule_WhenModuleHasLessons_ShouldReturnConflict()
    {
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();

        var response = await client.DeleteAsync($"/api/courses/{course.CourseId}/modules/{course.ModuleId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ReorderCourseModules_WhenIdSetDoesNotMatch_ShouldReturnBadRequest()
    {
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/courses/{course.CourseId}/modules/reorder",
            new { moduleIds = new[] { Guid.NewGuid() } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCourseModule_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();
        var course = await _factory.SeedPublishedCourseWithoutContentAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/courses/{course.CourseId}/modules",
            new { title = "Module", description = "Description" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCourseModule_WhenUserHasNoManageCoursesPermission_ShouldReturnForbidden()
    {
        using var client = CreateClient();
        var user = await _factory.SeedUserAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);
        var course = await _factory.SeedPublishedCourseWithoutContentAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/courses/{course.CourseId}/modules",
            new { title = "Module", description = "Description" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LessonVideoLifecycle_WhenAdmin_ShouldRoundTripWithYouTubeProvider()
    {
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();

        var getBeforeResponse = await client.GetAsync($"/api/videos/lessons/{course.LessonId}");
        Assert.Equal(HttpStatusCode.NotFound, getBeforeResponse.StatusCode);

        var replaceResponse = await client.PutAsJsonAsync(
            $"/api/videos/lessons/{course.LessonId}",
            new
            {
                title = "Lesson Video",
                description = "Description",
                storageProvider = "YouTube",
                storageKey = "dQw4w9WgXcQ",
                durationSeconds = 300,
                sizeBytes = 0
            });
        await AssertStatusAsync(HttpStatusCode.OK, replaceResponse);
        var video = await replaceResponse.Content.ReadFromJsonAsync<VideoResponse>();
        Assert.NotNull(video);
        Assert.Equal("YouTube", video!.StorageProvider);

        var getAfterResponse = await client.GetAsync($"/api/videos/lessons/{course.LessonId}");
        await AssertStatusAsync(HttpStatusCode.OK, getAfterResponse);

        var removeResponse = await client.DeleteAsync($"/api/videos/lessons/{course.LessonId}");
        await AssertStatusAsync(HttpStatusCode.NoContent, removeResponse);

        var getFinalResponse = await client.GetAsync($"/api/videos/lessons/{course.LessonId}");
        Assert.Equal(HttpStatusCode.NotFound, getFinalResponse.StatusCode);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    private static async Task AssertStatusAsync(HttpStatusCode expected, HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == expected,
            $"Expected {expected} but received {response.StatusCode}. Body: {content}");
    }
}
