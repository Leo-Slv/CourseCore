using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Tests.Integration.Infrastructure;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;

namespace CourseCore.Api.Tests.Integration.Auth;

public class ForgotPasswordIntegrationTests : IClassFixture<CourseCoreApiFactory>
{
    private readonly CourseCoreApiFactory _factory;

    public ForgotPasswordIntegrationTests(CourseCoreApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ForgotPassword_WhenEmailIsUnknown_ShouldReturnNoContentAndNotSendEmail()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var emailSender = (FakeEmailSender)_factory.Services.GetRequiredService<IEmailSender>();
        var sentBefore = emailSender.Sent.Count;

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = $"unknown.{Guid.NewGuid():N}@coursecore.local",
            captchaToken = "any-token"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(sentBefore, emailSender.Sent.Count);
    }

    [Fact]
    public async Task ForgotPassword_WhenEmailIsKnown_ShouldSendEmailWithResetLink()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        var emailSender = (FakeEmailSender)_factory.Services.GetRequiredService<IEmailSender>();

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = user.Email,
            captchaToken = "any-token"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var email = emailSender.Sent.Last();
        Assert.Equal(user.Email, email.To);
        Assert.Contains("reset-password?token=", email.HtmlBody);
    }

    [Fact]
    public async Task ResetPassword_FullRoundTrip_ShouldChangePasswordAndInvalidateOldSessions()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        var emailSender = (FakeEmailSender)_factory.Services.GetRequiredService<IEmailSender>();

        await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = user.Email,
            captchaToken = "any-token"
        });
        var token = ExtractResetToken(emailSender);

        var resetResponse = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token,
            newPassword = "New_password_789!"
        });
        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        var oldLogin = await IntegrationAuth.LoginAsync(client, user.Email, user.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await IntegrationAuth.LoginAsync(client, user.Email, "New_password_789!");
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WhenTokenIsReused_ShouldReturnBadRequestOnSecondAttempt()
    {
        using var client = IntegrationAuth.CreateClient(_factory);
        var user = await _factory.SeedUserAsync();
        var emailSender = (FakeEmailSender)_factory.Services.GetRequiredService<IEmailSender>();

        await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = user.Email,
            captchaToken = "any-token"
        });
        var token = ExtractResetToken(emailSender);

        await client.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword = "New_password_789!" });
        var secondAttempt = await client.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword = "Another_password_012!" });

        Assert.Equal(HttpStatusCode.BadRequest, secondAttempt.StatusCode);
    }

    private static string ExtractResetToken(FakeEmailSender emailSender)
    {
        var lastEmail = emailSender.Sent[^1];
        var match = Regex.Match(lastEmail.HtmlBody, "<strong>(.+?)</strong>");

        return match.Groups[1].Value;
    }
}
