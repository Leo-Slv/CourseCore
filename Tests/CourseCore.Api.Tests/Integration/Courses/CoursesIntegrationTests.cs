using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CourseCore.Api.Tests.Integration.Courses;

public class CoursesIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public CoursesIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCourseDetails_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();
        var courseId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/courses/{courseId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCourse_WhenAdminPostsValidRequest_ShouldReturnCreated()
    {
        using var client = CreateClient();
        var areaId = await _factory.SeedAreaAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/courses", CreateCourseRequest(areaId));

        await AssertStatusAsync(HttpStatusCode.Created, response);
    }

    [Fact]
    public async Task CreateCourse_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/courses", CreateCourseRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCourse_WhenUserHasNoPermissionOrAdminRole_ShouldReturnForbidden()
    {
        using var client = CreateClient();
        var user = await _factory.SeedUserAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/courses", CreateCourseRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCourse_WhenUserHasManageCoursesPermission_ShouldReturnCreated()
    {
        using var client = CreateClient();
        var areaId = await _factory.SeedAreaAsync();
        var user = await _factory.SeedUserAsync(AuthPermissionNames.ManageCourses);
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.PostAsJsonAsync("/api/courses", CreateCourseRequest(areaId));

        await AssertStatusAsync(HttpStatusCode.Created, response);
    }

    [Fact]
    public async Task CreateCourse_WhenRequestIsInvalid_ShouldReturnBadRequest()
    {
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/courses", new
        {
            title = string.Empty,
            slug = $"invalid-course-{Guid.NewGuid():N}",
            description = "Invalid integration course",
            displayOrder = 0,
            areaIds = Array.Empty<Guid>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCourse_WhenAdminPostsValidRequest_ShouldReturnOk()
    {
        using var client = CreateClient();
        var areaId = await _factory.SeedAreaAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PutAsJsonAsync($"/api/courses/{course.CourseId}", new
        {
            title = "Updated Integration Course",
            slug = $"updated-integration-course-{Guid.NewGuid():N}",
            description = "Updated integration course",
            thumbnailUrl = "https://cdn.coursecore.local/thumb.png",
            displayOrder = 1,
            areaIds = new[] { areaId }
        });

        await AssertStatusAsync(HttpStatusCode.OK, response);
    }

    [Fact]
    public async Task PublishCourse_WhenAdminPostsValidRequest_ShouldReturnOk()
    {
        using var client = CreateClient();
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsync($"/api/courses/{course.CourseId}/publish", content: null);

        await AssertStatusAsync(HttpStatusCode.OK, response);
    }

    [Fact]
    public async Task GetCourseDetails_WhenUserHasNoAccess_ShouldReturnForbidden()
    {
        using var client = CreateClient();
        var courseId = await _factory.SeedPublishedCourseAsync(grantAdminAccess: false);
        var login = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.GetAsync($"/api/courses/{courseId}");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCourseDetails_WhenUserHasAccess_ShouldReturnOk()
    {
        using var client = CreateClient();
        var courseId = await _factory.SeedPublishedCourseAsync(grantAdminAccess: true);
        var login = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.GetAsync($"/api/courses/{courseId}");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(courseId, json.RootElement.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetCourseDetails_WhenCourseDoesNotExist_ShouldReturnNotFound()
    {
        using var client = CreateClient();
        var user = await _factory.SeedUserAsync();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync($"/api/courses/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListAvailableCourses_WhenUserHasAccess_ShouldReturnOk()
    {
        var user = await _factory.SeedUserAsync();
        await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync("/api/courses/available");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    private static async Task<AuthTokenResult> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CourseCoreApiFactory.AdminEmail,
            password = CourseCoreApiFactory.AdminPassword
        });

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new AuthTokenResult(response.StatusCode, string.Empty);
        }

        using var json = JsonDocument.Parse(content);
        var token = json.RootElement.GetProperty("token");

        return new AuthTokenResult(
            response.StatusCode,
            token.GetProperty("accessToken").GetString() ?? string.Empty);
    }

    private sealed record AuthTokenResult(HttpStatusCode StatusCode, string AccessToken);

    private static async Task AssertStatusAsync(HttpStatusCode expected, HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == expected,
            $"Expected {expected} but received {response.StatusCode}. Body: {content}");
    }

    private static object CreateCourseRequest(Guid areaId)
    {
        return new
        {
            title = "Created Integration Course",
            slug = $"created-integration-course-{Guid.NewGuid():N}",
            description = "Created integration course",
            thumbnailUrl = "https://cdn.coursecore.local/course.png",
            displayOrder = 0,
            areaIds = new[] { areaId },
            modules = new[]
            {
                new
                {
                    title = "Created Integration Module",
                    description = "Created integration module",
                    displayOrder = 0,
                    lessons = new[]
                    {
                        new
                        {
                            title = "Created Integration Lesson",
                            description = "Created integration lesson",
                            displayOrder = 0,
                            freePreview = false
                        }
                    }
                }
            }
        };
    }
}
