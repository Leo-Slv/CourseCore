using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Auth;

public class ConfirmEmailUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTokenIsValid_ShouldMarkEmailAsVerified()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(emailVerified: false);
        fixture.Users.Add(user);
        AddToken(fixture, user.Id, "raw-token");

        await fixture.UseCase.ExecuteAsync(new ConfirmEmailInput { UserId = user.Id, Token = "raw-token" });

        var updated = await fixture.Users.FindByIdAsync(user.Id);
        Assert.NotNull(updated!.EmailVerifiedAt);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenIsValid_ShouldRecordAuditLog()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(emailVerified: false);
        fixture.Users.Add(user);
        AddToken(fixture, user.Id, "raw-token");

        await fixture.UseCase.ExecuteAsync(new ConfirmEmailInput { UserId = user.Id, Token = "raw-token" });

        var auditLog = Assert.Single(fixture.AuditLogs.Entries);
        Assert.Equal(AuditLogActionNames.UserEmailVerified, auditLog.Action);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenDoesNotExist_ShouldThrow()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(emailVerified: false);
        fixture.Users.Add(user);

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => fixture.UseCase.ExecuteAsync(new ConfirmEmailInput { UserId = user.Id, Token = "missing-token" }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenIsExpired_ShouldThrow()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(emailVerified: false);
        fixture.Users.Add(user);
        var expiredToken = EmailVerificationToken.Create(user.Id, "hash:raw-token", DateTime.UtcNow.AddMinutes(-1));
        fixture.EmailVerificationTokens.AddExisting(expiredToken);

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => fixture.UseCase.ExecuteAsync(new ConfirmEmailInput { UserId = user.Id, Token = "raw-token" }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenBelongsToAnotherUser_ShouldThrow()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(emailVerified: false);
        var otherUser = TestEntityFactory.User(id: Guid.NewGuid(), email: "other@coursecore.local", emailVerified: false);
        fixture.Users.Add(user);
        fixture.Users.Add(otherUser);
        AddToken(fixture, otherUser.Id, "raw-token");

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => fixture.UseCase.ExecuteAsync(new ConfirmEmailInput { UserId = user.Id, Token = "raw-token" }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenWasAlreadyConsumed_ShouldThrow()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(emailVerified: false);
        fixture.Users.Add(user);
        var token = AddToken(fixture, user.Id, "raw-token");
        token.Consume();

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => fixture.UseCase.ExecuteAsync(new ConfirmEmailInput { UserId = user.Id, Token = "raw-token" }));
    }

    private static EmailVerificationToken AddToken(ConfirmEmailFixture fixture, Guid userId, string rawToken)
    {
        var token = EmailVerificationToken.Create(
            userId,
            fixture.Hasher.Hash(rawToken),
            DateTime.UtcNow.AddHours(1));

        fixture.EmailVerificationTokens.AddExisting(token);

        return token;
    }

    private static ConfirmEmailFixture CreateFixture()
    {
        var users = new FakeUserRepository();
        var emailVerificationTokens = new FakeEmailVerificationTokenRepository();
        var hasher = new FakeEmailVerificationTokenHasher();
        var unitOfWork = new FakeUnitOfWork();
        var auditLogs = new FakeAuditLogService();

        var useCase = new ConfirmEmailUseCase(users, emailVerificationTokens, hasher, unitOfWork, auditLogs);

        return new ConfirmEmailFixture(useCase, users, emailVerificationTokens, hasher, auditLogs);
    }

    private sealed record ConfirmEmailFixture(
        ConfirmEmailUseCase UseCase,
        FakeUserRepository Users,
        FakeEmailVerificationTokenRepository EmailVerificationTokens,
        FakeEmailVerificationTokenHasher Hasher,
        FakeAuditLogService AuditLogs);
}
