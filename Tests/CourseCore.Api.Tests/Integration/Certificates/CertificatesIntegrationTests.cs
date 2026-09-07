using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Modules.Certificates.Presentation.Responses;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Certificates;

public class CertificatesIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public CertificatesIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyCertificates_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.GetAsync("/api/certificates/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyCertificates_AfterCompletingCourse_ShouldReturnIssuedCertificate()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        await _factory.SeedReadyVideoAsync(course.LessonId, durationSeconds: 100);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        await client.PostAsJsonAsync("/api/progress/lessons", new
        {
            lessonId = course.LessonId,
            watchedSeconds = 90
        });

        var response = await client.GetAsync("/api/certificates/mine");
        var body = await response.Content.ReadFromJsonAsync<List<CertificateResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        var certificate = Assert.Single(body!);
        Assert.Equal(course.CourseId, certificate.CourseId);
    }

    [Fact]
    public async Task GetMyCertificates_WhenCourseNotCompleted_ShouldReturnEmpty()
    {
        var user = await _factory.SeedUserAsync();
        await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync("/api/certificates/mine");
        var body = await response.Content.ReadFromJsonAsync<List<CertificateResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Empty(body!);
    }
}
