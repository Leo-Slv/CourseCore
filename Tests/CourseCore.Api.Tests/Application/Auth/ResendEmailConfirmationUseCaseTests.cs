using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Auth;

public class ResendEmailConfirmationUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUserIsUnverified_ShouldInvalidatePreviousTokenAndSendNewOne()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(emailVerified: false);
        fixture.Users.Add(user);
        var previousToken = EmailVerificationToken.Create(user.Id, "hash:old-token", DateTime.UtcNow.AddHours(1));
        fixture.EmailVerificationTokens.AddExisting(previousToken);

        await fixture.UseCase.ExecuteAsync(user.Id);

        Assert.True(previousToken.IsConsumed);
        var newToken = Assert.Single(fixture.EmailVerificationTokens.Added);
        Assert.Equal(user.Id, newToken.UserId);
        var email = Assert.Single(fixture.EmailSender.Sent);
        Assert.Equal(user.Email.Value, email.To);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsUnverified_ShouldRecordAuditLog()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(emailVerified: false);
        fixture.Users.Add(user);

        await fixture.UseCase.ExecuteAsync(user.Id);

        var auditLog = Assert.Single(fixture.AuditLogs.Entries);
        Assert.Equal(AuditLogActionNames.EmailVerificationResent, auditLog.Action);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailIsAlreadyVerified_ShouldThrowConflict()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(emailVerified: true);
        fixture.Users.Add(user);

        await Assert.ThrowsAsync<ConflictException>(() => fixture.UseCase.ExecuteAsync(user.Id));

        Assert.Empty(fixture.EmailSender.Sent);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowNotFound()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(Guid.NewGuid()));
    }

    private static ResendFixture CreateFixture()
    {
        var users = new FakeUserRepository();
        var emailVerificationTokens = new FakeEmailVerificationTokenRepository();
        var unitOfWork = new FakeUnitOfWork();
        var auditLogs = new FakeAuditLogService();
        var emailSender = new FakeEmailSender();

        var useCase = new ResendEmailConfirmationUseCase(
            users,
            emailVerificationTokens,
            new FakeEmailVerificationTokenHasher(),
            new FakeEmailVerificationTokenGenerator("new-verification-token"),
            emailSender,
            unitOfWork,
            auditLogs);

        return new ResendFixture(useCase, users, emailVerificationTokens, emailSender, auditLogs);
    }

    private sealed record ResendFixture(
        ResendEmailConfirmationUseCase UseCase,
        FakeUserRepository Users,
        FakeEmailVerificationTokenRepository EmailVerificationTokens,
        FakeEmailSender EmailSender,
        FakeAuditLogService AuditLogs);
}
