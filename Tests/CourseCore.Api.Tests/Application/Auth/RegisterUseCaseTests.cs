using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Application.Services;
using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Application.Auth;

public class RegisterUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_ShouldCreateUserWithoutRolesAndReturnSession()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(ValidInput());

        Assert.NotEqual(Guid.Empty, output.UserId);
        Assert.Empty(output.Roles);
        Assert.Equal("access-token-1", output.Token.AccessToken);
        Assert.Equal("refresh-token", output.Token.RefreshToken);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_ShouldCreateUserWithUnverifiedEmail()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(ValidInput());
        var user = await fixture.Users.FindByIdAsync(output.UserId);

        Assert.NotNull(user);
        Assert.Null(user.EmailVerifiedAt);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_ShouldPersistEmailVerificationTokenAndSendEmail()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(ValidInput());

        var token = Assert.Single(fixture.EmailVerificationTokens.Added);
        Assert.Equal(output.UserId, token.UserId);
        Assert.Equal("hash:verification-token", token.TokenHash);

        var email = Assert.Single(fixture.EmailSender.Sent);
        Assert.Equal("new.user@coursecore.local", email.To);
        Assert.Contains("verification-token", email.HtmlBody);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_ShouldRecordUserRegisteredAuditLog()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(ValidInput());

        var auditLog = Assert.Single(fixture.AuditLogs.Entries);
        Assert.Equal(AuditLogActionNames.UserRegistered, auditLog.Action);
        Assert.Equal(output.UserId, auditLog.EntityId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCaptchaIsInvalid_ShouldThrowAndNotCreateUser()
    {
        var fixture = CreateFixture();
        fixture.Captcha.Result = false;

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => fixture.UseCase.ExecuteAsync(ValidInput()));

        Assert.Equal(0, fixture.UnitOfWork.ExecuteCalls);
        Assert.Empty(fixture.EmailSender.Sent);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailAlreadyExists_ShouldThrowConflict()
    {
        var fixture = CreateFixture();
        fixture.Users.Add(TestEntityFactory.User(email: "new.user@coursecore.local"));

        await Assert.ThrowsAsync<ConflictException>(
            () => fixture.UseCase.ExecuteAsync(ValidInput()));

        Assert.Empty(fixture.EmailSender.Sent);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordIsWeak_ShouldThrow()
    {
        var fixture = CreateFixture();
        var input = new RegisterInput
        {
            Name = "New User",
            Email = "new.user@coursecore.local",
            Password = "weak",
            CaptchaToken = "captcha-token"
        };

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => fixture.UseCase.ExecuteAsync(input));
    }

    private static RegisterInput ValidInput()
    {
        return new RegisterInput
        {
            Name = "New User",
            Email = "new.user@coursecore.local",
            Password = "Change_me_123456!",
            CaptchaToken = "captcha-token"
        };
    }

    private static RegisterFixture CreateFixture()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var refreshTokens = new FakeRefreshTokenRepository();
        var emailVerificationTokens = new FakeEmailVerificationTokenRepository();
        var unitOfWork = new FakeUnitOfWork();
        var auditLogs = new FakeAuditLogService();
        var captcha = new FakeCaptchaVerificationService();
        var emailSender = new FakeEmailSender();

        var sessionIssuer = new SessionIssuer(
            roles,
            new FakeTokenService(),
            new FakeRefreshTokenGenerator("refresh-token"),
            new FakeRefreshTokenHasher(),
            Options.Create(new JwtOptions
            {
                AccessTokenExpirationMinutes = 60,
                RefreshTokenExpirationDays = 7
            }));

        var useCase = new RegisterUseCase(
            users,
            new FakePasswordHasher(),
            new PasswordPolicy(),
            captcha,
            emailVerificationTokens,
            new FakeEmailVerificationTokenHasher(),
            new FakeEmailVerificationTokenGenerator("verification-token"),
            refreshTokens,
            sessionIssuer,
            emailSender,
            unitOfWork,
            auditLogs);

        return new RegisterFixture(useCase, users, emailVerificationTokens, emailSender, unitOfWork, auditLogs, captcha);
    }

    private sealed record RegisterFixture(
        RegisterUseCase UseCase,
        FakeUserRepository Users,
        FakeEmailVerificationTokenRepository EmailVerificationTokens,
        FakeEmailSender EmailSender,
        FakeUnitOfWork UnitOfWork,
        FakeAuditLogService AuditLogs,
        FakeCaptchaVerificationService Captcha);
}
