using System.Net;
using System.Net.Http.Json;
using CourseCore.Api.Modules.Testimonials.Presentation.Responses;
using CourseCore.Api.Tests.Integration.Infrastructure;

namespace CourseCore.Api.Tests.Integration.Testimonials;

public class TestimonialsIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public TestimonialsIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TestimonialLifecycle_WhenAdmin_ShouldRoundTripAndAppearInPublicList()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/testimonials", new
        {
            authorName = "Marina Souza",
            quote = "Great course!"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<TestimonialResponse>();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);
        Assert.False(created!.Published);

        using var anonymousClient = IntegrationAuth.CreateClient(_factory);
        var beforePublishResponse = await anonymousClient.GetAsync("/api/testimonials/public");
        var beforePublishBody = await beforePublishResponse.Content.ReadFromJsonAsync<List<TestimonialResponse>>();
        Assert.DoesNotContain(beforePublishBody!, t => t.Id == created.Id);

        var publishResponse = await client.PostAsync($"/api/testimonials/{created.Id}/publish", content: null);
        var published = await publishResponse.Content.ReadFromJsonAsync<TestimonialResponse>();

        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        Assert.NotNull(published);
        Assert.True(published!.Published);

        var afterPublishResponse = await anonymousClient.GetAsync("/api/testimonials/public");
        var afterPublishBody = await afterPublishResponse.Content.ReadFromJsonAsync<List<TestimonialResponse>>();

        Assert.Equal(HttpStatusCode.OK, afterPublishResponse.StatusCode);
        Assert.Contains(afterPublishBody!, t => t.Id == created.Id);

        var updateResponse = await client.PutAsJsonAsync($"/api/testimonials/{created.Id}", new
        {
            authorName = "Marina Souza Updated",
            quote = "Updated quote"
        });
        var updated = await updateResponse.Content.ReadFromJsonAsync<TestimonialResponse>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("Marina Souza Updated", updated!.AuthorName);

        var unpublishResponse = await client.PostAsync($"/api/testimonials/{created.Id}/unpublish", content: null);
        Assert.Equal(HttpStatusCode.OK, unpublishResponse.StatusCode);

        var afterUnpublishResponse = await anonymousClient.GetAsync("/api/testimonials/public");
        var afterUnpublishBody = await afterUnpublishResponse.Content.ReadFromJsonAsync<List<TestimonialResponse>>();
        Assert.DoesNotContain(afterUnpublishBody!, t => t.Id == created.Id);

        var listResponse = await client.GetAsync("/api/testimonials");
        var listBody = await listResponse.Content.ReadFromJsonAsync<List<TestimonialResponse>>();

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains(listBody!, t => t.Id == created.Id);
    }

    [Fact]
    public async Task CreateTestimonial_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync("/api/testimonials", new
        {
            authorName = "Author",
            quote = "Quote"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTestimonial_WithCourseReference_ShouldPersistCourseId()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var course = await _factory.SeedPublishedCourseWithLessonAsync();

        var response = await client.PostAsJsonAsync("/api/testimonials", new
        {
            authorName = "Carlos Andrade",
            quote = "Loved the course",
            courseId = course.CourseId
        });
        var body = await response.Content.ReadFromJsonAsync<TestimonialResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(course.CourseId, body!.CourseId);
    }

    [Fact]
    public async Task GetPublicTestimonials_ShouldNotRequireAuthentication()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.GetAsync("/api/testimonials/public");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SubmitMine_WhenAuthenticatedNonAdmin_ShouldCreateUnpublishedTestimonialWithOwnIdentity()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/testimonials/mine", new
        {
            quote = "This platform changed how I study!"
        });
        var body = await response.Content.ReadFromJsonAsync<TestimonialResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Integration User", body!.AuthorName);
        Assert.False(body.Published);
    }

    [Fact]
    public async Task SubmitMine_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync("/api/testimonials/mine", new
        {
            quote = "Quote"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTestimonial_WhenAuthenticatedNonAdmin_ShouldReturnForbidden()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/testimonials", new
        {
            authorName = "Impersonator",
            quote = "Quote"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
