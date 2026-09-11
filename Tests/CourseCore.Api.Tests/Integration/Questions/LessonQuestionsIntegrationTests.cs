using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Modules.Questions.Presentation.Responses;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Questions;

public class LessonQuestionsIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public LessonQuestionsIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AskQuestion_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync(
            $"/api/questions/lessons/{Guid.NewGuid()}",
            new { questionText = "Question?" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AskQuestion_WhenUserHasNoCourseAccess_ShouldReturnForbidden()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync(
            $"/api/questions/lessons/{course.LessonId}",
            new { questionText = "Question?" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AskQuestion_WhenUserHasAccess_ShouldReturnCreatedAndAppearInList()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var askResponse = await client.PostAsJsonAsync(
            $"/api/questions/lessons/{course.LessonId}",
            new { questionText = "How does this work?" });
        var created = await askResponse.Content.ReadFromJsonAsync<LessonQuestionResponse>();

        Assert.Equal(HttpStatusCode.Created, askResponse.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("How does this work?", created!.QuestionText);
        Assert.Null(created.AnswerText);

        var listResponse = await client.GetAsync($"/api/questions/lessons/{course.LessonId}");
        var list = await listResponse.Content.ReadFromJsonAsync<List<LessonQuestionResponse>>();

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(list);
        Assert.Contains(list!, question => question.Id == created.Id);
    }

    [Fact]
    public async Task AnswerQuestion_WhenStudentAttempts_ShouldReturnForbidden()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);
        var askResponse = await client.PostAsJsonAsync(
            $"/api/questions/lessons/{course.LessonId}",
            new { questionText = "Question?" });
        var created = await askResponse.Content.ReadFromJsonAsync<LessonQuestionResponse>();
        Assert.NotNull(created);

        var response = await client.PostAsJsonAsync(
            $"/api/questions/{created!.Id}/answer",
            new { answerText = "Answer" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnswerQuestion_WhenAdmin_ShouldReturnOkWithAnswerFields()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var studentClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(studentClient, user);
        var askResponse = await studentClient.PostAsJsonAsync(
            $"/api/questions/lessons/{course.LessonId}",
            new { questionText = "Question?" });
        var created = await askResponse.Content.ReadFromJsonAsync<LessonQuestionResponse>();
        Assert.NotNull(created);

        using var adminClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/questions/{created!.Id}/answer",
            new { answerText = "Here is the answer." });
        var body = await response.Content.ReadFromJsonAsync<LessonQuestionResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Here is the answer.", body!.AnswerText);
        Assert.NotNull(body.AnsweredAt);
    }

    [Fact]
    public async Task RemoveQuestion_WhenAdmin_ShouldReturnNoContentAndRemoveFromList()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var studentClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(studentClient, user);
        var askResponse = await studentClient.PostAsJsonAsync(
            $"/api/questions/lessons/{course.LessonId}",
            new { questionText = "Question?" });
        var created = await askResponse.Content.ReadFromJsonAsync<LessonQuestionResponse>();
        Assert.NotNull(created);

        using var adminClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);

        var deleteResponse = await adminClient.DeleteAsync($"/api/questions/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var listResponse = await studentClient.GetAsync($"/api/questions/lessons/{course.LessonId}");
        var list = await listResponse.Content.ReadFromJsonAsync<List<LessonQuestionResponse>>();
        Assert.NotNull(list);
        Assert.DoesNotContain(list!, question => question.Id == created.Id);
    }

    [Fact]
    public async Task RemoveQuestion_WhenQuestionDoesNotExist_ShouldReturnNotFound()
    {
        using var adminClient = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(adminClient);

        var response = await adminClient.DeleteAsync($"/api/questions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
