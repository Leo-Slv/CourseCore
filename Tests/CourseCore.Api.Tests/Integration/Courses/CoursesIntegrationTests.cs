using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Tests.Integration.Infrastructure;
using CourseCore.Api.Modules.Courses.Application.Validation;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Modules.Courses.Presentation.Responses;
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
    public async Task CreateCourse_WhenFreeWithPriceAmount_ShouldReturnBadRequest()
    {
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/courses", new
        {
            title = "Free Course With Price",
            slug = $"free-course-with-price-{Guid.NewGuid():N}",
            description = "Description",
            displayOrder = 0,
            pricingModel = "Free",
            priceAmount = 50m,
            areaIds = Array.Empty<Guid>(),
            modules = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCourse_WhenTitleIsTooLong_ShouldReturnBadRequest()
    {
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/courses", new
        {
            title = new string('T', CourseValidationLimits.TitleMaxLength + 1),
            slug = $"large-title-{Guid.NewGuid():N}",
            description = "Description",
            areaIds = Array.Empty<Guid>(),
            modules = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCourse_WhenModulesExceedLimit_ShouldReturnBadRequest()
    {
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var modules = Enumerable.Range(0, CourseValidationLimits.MaxModules + 1)
            .Select(index => new { title = $"Module {index}", description = "Description", lessons = Array.Empty<object>() });

        var response = await client.PostAsJsonAsync("/api/courses", new
        {
            title = "Oversized Course",
            slug = $"oversized-{Guid.NewGuid():N}",
            description = "Description",
            areaIds = Array.Empty<Guid>(),
            modules
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCourse_WhenAreaIdsExceedLimit_ShouldReturnBadRequest()
    {
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/courses", new
        {
            title = "Oversized Areas Course",
            slug = $"oversized-areas-{Guid.NewGuid():N}",
            description = "Description",
            areaIds = Enumerable.Range(0, CourseValidationLimits.MaxAreaIds + 1).Select(_ => Guid.NewGuid()),
            modules = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCourse_WhenModuleHasTooManyLessons_ShouldReturnBadRequest()
    {
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);
        var lessons = Enumerable.Range(0, CourseValidationLimits.MaxLessonsPerModule + 1)
            .Select(index => new { title = $"Lesson {index}", description = "Description" });

        var response = await client.PostAsJsonAsync("/api/courses", new
        {
            title = "Oversized Lessons Course",
            slug = $"oversized-lessons-{Guid.NewGuid():N}",
            description = "Description",
            areaIds = Array.Empty<Guid>(),
            modules = new[] { new { title = "Module", description = "Description", lessons } }
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
            pricingModel = "Paid",
            areaIds = new[] { areaId }
        });

        await AssertStatusAsync(HttpStatusCode.OK, response);
    }

    [Fact]
    public async Task UpdateCourse_WhenPriceAmountProvided_ShouldPersistPriceAmount()
    {
        using var client = CreateClient();
        var areaId = await _factory.SeedAreaAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync();
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var updateResponse = await client.PutAsJsonAsync($"/api/courses/{course.CourseId}", new
        {
            title = "Updated Integration Course",
            slug = $"updated-integration-course-{Guid.NewGuid():N}",
            description = "Updated integration course",
            thumbnailUrl = "https://cdn.coursecore.local/thumb.png",
            displayOrder = 1,
            pricingModel = "Paid",
            priceAmount = 199.90m,
            areaIds = new[] { areaId }
        });

        await AssertStatusAsync(HttpStatusCode.OK, updateResponse);

        var viewer = await _factory.SeedUserAsync();
        using var viewerClient = CreateClient();
        await IntegrationAuth.AuthenticateAsAsync(viewerClient, viewer);

        var catalogResponse = await viewerClient.GetAsync("/api/courses/available");
        var body = await catalogResponse.Content.ReadFromJsonAsync<CourseCatalogResponse>();

        Assert.NotNull(body);
        Assert.Equal(199.90m, body!.Courses.Single(c => c.Id == course.CourseId).PriceAmount);
    }

    [Fact]
    public async Task ListAvailableCourses_ShouldIncludePriceAmountOnPaidCourses()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(pricingModel: CoursePricingModel.Paid);
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync("/api/courses/available");
        var body = await response.Content.ReadFromJsonAsync<CourseCatalogResponse>();

        Assert.NotNull(body);
        Assert.Null(body!.Courses.Single(c => c.Id == course.CourseId).PriceAmount);
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
    public async Task GetCourseDetails_WhenLessonHasVideo_ShouldIncludeVideoIdAndDuration()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        var video = await _factory.SeedReadyVideoAsync(course.LessonId, durationSeconds: 240);
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync($"/api/courses/{course.CourseId}");
        var body = await response.Content.ReadFromJsonAsync<CourseDetailsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        var lesson = Assert.Single(Assert.Single(body!.Modules).Lessons);
        Assert.Equal(video.VideoId, lesson.VideoId);
        Assert.Equal(240, lesson.DurationSeconds);

        var playbackResponse = await client.GetAsync($"/api/videos/{lesson.VideoId}/playback");

        Assert.Equal(HttpStatusCode.OK, playbackResponse.StatusCode);
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

    [Fact]
    public async Task ListAvailableCourses_ShouldIncludeGrantedAndLockedCoursesWithAreas()
    {
        var user = await _factory.SeedUserAsync();
        var granted = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        var locked = await _factory.SeedPublishedCourseWithLessonAsync();
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync("/api/courses/available");
        var body = await response.Content.ReadFromJsonAsync<CourseCatalogResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body!.Areas.Count >= 2);
        Assert.True(body.Courses.Single(c => c.Id == granted.CourseId).HasAccess);
        Assert.False(body.Courses.Single(c => c.Id == locked.CourseId).HasAccess);
    }

    [Fact]
    public async Task ListAvailableCourses_WhenFilteredByHasAccessTrue_ShouldOnlyReturnGrantedCourses()
    {
        var user = await _factory.SeedUserAsync();
        var granted = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        var locked = await _factory.SeedPublishedCourseWithLessonAsync();
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync("/api/courses/available?hasAccess=true");
        var body = await response.Content.ReadFromJsonAsync<CourseCatalogResponse>();

        Assert.NotNull(body);
        Assert.Contains(body!.Courses, c => c.Id == granted.CourseId);
        Assert.DoesNotContain(body.Courses, c => c.Id == locked.CourseId);
    }

    [Fact]
    public async Task ListAvailableCourses_WhenCourseIsFree_ShouldMarkFreeCourseAsAccessible()
    {
        var user = await _factory.SeedUserAsync();
        var freeCourse = await _factory.SeedPublishedCourseWithLessonAsync(pricingModel: CoursePricingModel.Free);
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync("/api/courses/available");
        var body = await response.Content.ReadFromJsonAsync<CourseCatalogResponse>();

        Assert.NotNull(body);
        Assert.True(body!.Courses.Single(c => c.Id == freeCourse.CourseId).HasAccess);
    }

    [Fact]
    public async Task ListAvailableCourses_ShouldIncludeModuleLessonCountsAndSummedDuration()
    {
        var user = await _factory.SeedUserAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(user.Id);
        await _factory.SeedReadyVideoAsync(course.LessonId, durationSeconds: 300);

        var secondModuleId = await _factory.SeedCourseModuleAsync(course.CourseId, displayOrder: 1);
        var secondLessonId = await _factory.SeedLessonAsync(secondModuleId, displayOrder: 0);
        var thirdLessonId = await _factory.SeedLessonAsync(secondModuleId, displayOrder: 1);
        await _factory.SeedReadyVideoAsync(secondLessonId, durationSeconds: 180);
        _ = thirdLessonId; // intentionally left without a video

        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync("/api/courses/available");
        var body = await response.Content.ReadFromJsonAsync<CourseCatalogResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        var item = body!.Courses.Single(c => c.Id == course.CourseId);
        Assert.Equal(2, item.ModuleCount);
        Assert.Equal(3, item.LessonCount);
        Assert.Equal(480, item.DurationSeconds);
    }

    [Fact]
    public async Task ListAvailableCourses_WhenCourseHasNoModules_ShouldReturnZeroCountsAndZeroDuration()
    {
        var user = await _factory.SeedUserAsync();
        var (_, courseId) = await _factory.SeedPublishedCourseWithoutContentAsync(user.Id);
        using var client = CreateClient();
        await IntegrationAuth.AuthenticateAsAsync(client, user);

        var response = await client.GetAsync("/api/courses/available");
        var body = await response.Content.ReadFromJsonAsync<CourseCatalogResponse>();

        Assert.NotNull(body);
        var item = body!.Courses.Single(c => c.Id == courseId);
        Assert.Equal(0, item.ModuleCount);
        Assert.Equal(0, item.LessonCount);
        Assert.Equal(0, item.DurationSeconds);
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
            pricingModel = "Paid",
            priceAmount = 149.90m,
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
