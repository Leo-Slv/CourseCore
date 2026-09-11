using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Modules.Progress.Presentation.Responses;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Progress;

public class LessonNotesIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public LessonNotesIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpsertNote_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PutAsJsonAsync(
            $"/api/notes/lessons/{Guid.NewGuid()}",
            new { content = "My note" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpsertNote_WhenUserHasNoCourseAccess_ShouldReturnForbidden()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PutAsJsonAsync(
            $"/api/notes/lessons/{course.LessonId}",
            new { content = "My note" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpsertNote_WhenCreatingThenUpdating_ShouldRoundTripContent()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var createResponse = await client.PutAsJsonAsync(
            $"/api/notes/lessons/{course.LessonId}",
            new { content = "First version" });
        var created = await createResponse.Content.ReadFromJsonAsync<LessonNoteResponse>();

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("First version", created!.Content);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/notes/lessons/{course.LessonId}",
            new { content = "Second version" });
        var updated = await updateResponse.Content.ReadFromJsonAsync<LessonNoteResponse>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal("Second version", updated.Content);
    }

    [Fact]
    public async Task GetNote_WhenNoteExists_ShouldReturnIt()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);
        await client.PutAsJsonAsync($"/api/notes/lessons/{course.LessonId}", new { content = "My note" });

        var response = await client.GetAsync($"/api/notes/lessons/{course.LessonId}");
        var body = await response.Content.ReadFromJsonAsync<LessonNoteResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("My note", body!.Content);
    }

    [Fact]
    public async Task GetNote_WhenNoteDoesNotExist_ShouldReturnNotFound()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync($"/api/notes/lessons/{course.LessonId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RemoveNote_WhenNoteExists_ShouldRemoveItAndSubsequentGetReturnsNotFound()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);
        await client.PutAsJsonAsync($"/api/notes/lessons/{course.LessonId}", new { content = "My note" });

        var deleteResponse = await client.DeleteAsync($"/api/notes/lessons/{course.LessonId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var getResponse = await client.GetAsync($"/api/notes/lessons/{course.LessonId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task RemoveNote_WhenNoteDoesNotExist_ShouldReturnNotFound()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.DeleteAsync($"/api/notes/lessons/{course.LessonId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpsertNote_WhenContentIsWhitespace_ShouldReturnBadRequest()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PutAsJsonAsync(
            $"/api/notes/lessons/{course.LessonId}",
            new { content = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
