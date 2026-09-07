using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Application.Auth;

public class RequestPasswordResetUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCaptchaIsInvalid_ShouldThrowAndNotSendEmail()
    {
        var fixture = CreateFixture();
        fixture.Captcha.Result = false;

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => fixture.UseCase.ExecuteAsync(ValidInput()));

        Assert.Empty(fixture.EmailSender.Sent);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailDoesNotExist_ShouldNotThrowAndNotSendEmail()
    {
        var fixture = CreateFixture();

        await fixture.UseCase.ExecuteAsync(ValidInput());

        Assert.Empty(fixture.EmailSender.Sent);
        Assert.Empty(fixture.PasswordResetTokens.Added);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsInactive_ShouldNotSendEmail()
    {
        var fixture = CreateFixture();
        fixture.Users.Add(TestEntityFactory.User(email: "known.user@coursecore.local", active: false));

        await fixture.UseCase.ExecuteAsync(ValidInput());

        Assert.Empty(fixture.EmailSender.Sent);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldPersistTokenAndSendEmail()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(email: "known.user@coursecore.local");
        fixture.Users.Add(user);

        await fixture.UseCase.ExecuteAsync(ValidInput());

        var token = Assert.Single(fixture.PasswordResetTokens.Added);
        Assert.Equal(user.Id, token.UserId);
        Assert.Equal("hash:reset-token", token.TokenHash);

        var email = Assert.Single(fixture.EmailSender.Sent);
        Assert.Equal(user.Email.Value, email.To);
        Assert.Contains("reset-token", email.HtmlBody);
        Assert.Contains("reset-password?token=reset-token", email.HtmlBody);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldRecordAuditLog()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(email: "known.user@coursecore.local");
        fixture.Users.Add(user);

        await fixture.UseCase.ExecuteAsync(ValidInput());

        var auditLog = Assert.Single(fixture.AuditLogs.Entries);
        Assert.Equal(AuditLogActionNames.PasswordResetRequested, auditLog.Action);
        Assert.Equal(user.Id, auditLog.EntityId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalledTwice_ShouldInvalidatePreviousToken()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(email: "known.user@coursecore.local");
        fixture.Users.Add(user);
        var existingToken = PasswordResetToken.Create(user.Id, "hash:old-token", DateTime.UtcNow.AddHours(1));
        fixture.PasswordResetTokens.AddExisting(existingToken);

        await fixture.UseCase.ExecuteAsync(ValidInput());

        Assert.True(existingToken.IsConsumed);
    }

    private static RequestPasswordResetInput ValidInput()
    {
        return new RequestPasswordResetInput
        {
            Email = "known.user@coursecore.local",
            CaptchaToken = "captcha-token"
        };
    }

    private static RequestPasswordResetFixture CreateFixture()
    {
        var users = new FakeUserRepository();
        var captcha = new FakeCaptchaVerificationService();
        var passwordResetTokens = new FakePasswordResetTokenRepository();
        var emailSender = new FakeEmailSender();
        var unitOfWork = new FakeUnitOfWork();
        var auditLogs = new FakeAuditLogService();

        var useCase = new RequestPasswordResetUseCase(
            users,
            captcha,
            passwordResetTokens,
            new FakePasswordResetTokenHasher(),
            new FakePasswordResetTokenGenerator("reset-token"),
            emailSender,
            unitOfWork,
            auditLogs,
            Options.Create(new FrontendOptions { BaseUrl = "https://app.coursecore.local" }));

        return new RequestPasswordResetFixture(useCase, users, captcha, passwordResetTokens, emailSender, auditLogs);
    }

    private sealed record RequestPasswordResetFixture(
        RequestPasswordResetUseCase UseCase,
        FakeUserRepository Users,
        FakeCaptchaVerificationService Captcha,
        FakePasswordResetTokenRepository PasswordResetTokens,
        FakeEmailSender EmailSender,
        FakeAuditLogService AuditLogs);
}
