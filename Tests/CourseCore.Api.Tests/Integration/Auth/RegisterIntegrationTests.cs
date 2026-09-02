using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Tests.Integration.Infrastructure;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;

namespace CourseCore.Api.Tests.Integration.Auth;

public class RegisterIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public RegisterIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WhenRequestIsValid_ShouldReturnCreatedWithSessionAndRefreshCookie()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "New Student",
            email = $"student.{Guid.NewGuid():N}@coursecore.local",
            password = "Change_me_123456!",
            captchaToken = "any-token"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(response.Headers.Contains("Set-Cookie"));

        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.GetProperty("token").GetProperty("accessToken").GetString()!.Length > 0);
        Assert.Equal(0, json.RootElement.GetProperty("roles").GetArrayLength());
    }

    [Fact]
    public async Task Register_WhenCaptchaIsInvalid_ShouldReturnBadRequest()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var captcha = (FakeCaptchaVerificationService)_factory.Services.GetRequiredService<ICaptchaVerificationService>();
        captcha.Result = false;

        try
        {
            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                name = "New Student",
                email = $"student.{Guid.NewGuid():N}@coursecore.local",
                password = "Change_me_123456!",
                captchaToken = "invalid"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            captcha.Result = true;
        }
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ShouldReturnConflict()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var email = $"student.{Guid.NewGuid():N}@coursecore.local";
        var payload = new
        {
            name = "New Student",
            email,
            password = "Change_me_123456!",
            captchaToken = "any-token"
        };

        await client.PostAsJsonAsync("/api/auth/register", payload);
        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenPasswordIsWeak_ShouldReturnBadRequest()
    {
        using var client = IntegrationAuth.CreateClient(_factory);

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "New Student",
            email = $"student.{Guid.NewGuid():N}@coursecore.local",
            password = "weak",
            captchaToken = "any-token"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_WhenTokenIsValid_ShouldUnlockFreeCourseAccess()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var areaId = await _factory.SeedAreaAsync();
        var course = await _factory.SeedPublishedCourseWithLessonAsync(pricingModel: CoursePricingModel.Free);

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Free Course Student",
            email = $"student.{Guid.NewGuid():N}@coursecore.local",
            password = "Change_me_123456!",
            captchaToken = "any-token"
        });
        var registerBody = await register.Content.ReadAsStringAsync();
        using var registerJson = JsonDocument.Parse(registerBody);
        var accessToken = registerJson.RootElement.GetProperty("token").GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var beforeConfirm = await client.GetAsync($"/api/courses/{course.CourseId}");
        Assert.Equal(HttpStatusCode.Forbidden, beforeConfirm.StatusCode);

        var token = ExtractVerificationToken();
        var confirm = await client.PostAsJsonAsync("/api/auth/confirm-email", new { token });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        var afterConfirm = await client.GetAsync($"/api/courses/{course.CourseId}");
        Assert.Equal(HttpStatusCode.OK, afterConfirm.StatusCode);
    }

    [Fact]
    public async Task ResendConfirmation_WhenAlreadyVerified_ShouldReturnConflict()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        await IntegrationAuth.AuthenticateAsAdminAsync(client);

        var response = await client.PostAsync("/api/auth/resend-confirmation", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private string ExtractVerificationToken()
    {
        var emailSender = (FakeEmailSender)_factory.Services.GetRequiredService<IEmailSender>();
        var lastEmail = emailSender.Sent[^1];
        var match = Regex.Match(lastEmail.HtmlBody, "<strong>(.+?)</strong>");

        return match.Groups[1].Value;
    }
}
