using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Application.Services;
using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Auth;

public class ConfirmPasswordResetUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTokenIsInvalid_ShouldThrow()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(new ConfirmPasswordResetInput
        {
            Token = "unknown-token",
            NewPassword = "New_password_123!"
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenIsExpired_ShouldThrow()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User();
        fixture.Users.Add(user);
        var token = PasswordResetToken.Create(user.Id, "hash:expired-token", DateTime.UtcNow.AddMinutes(-1));
        fixture.PasswordResetTokens.AddExisting(token);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(new ConfirmPasswordResetInput
        {
            Token = "expired-token",
            NewPassword = "New_password_123!"
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenAlreadyConsumed_ShouldThrow()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User();
        fixture.Users.Add(user);
        var token = PasswordResetToken.Create(user.Id, "hash:used-token", DateTime.UtcNow.AddHours(1));
        token.Consume();
        fixture.PasswordResetTokens.AddExisting(token);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(new ConfirmPasswordResetInput
        {
            Token = "used-token",
            NewPassword = "New_password_123!"
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNewPasswordIsWeak_ShouldThrow()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User();
        fixture.Users.Add(user);
        var token = PasswordResetToken.Create(user.Id, "hash:valid-token", DateTime.UtcNow.AddHours(1));
        fixture.PasswordResetTokens.AddExisting(token);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(new ConfirmPasswordResetInput
        {
            Token = "valid-token",
            NewPassword = "weak"
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenIsValid_ShouldChangePasswordAndInvalidateSessions()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User();
        fixture.Users.Add(user);
        var token = PasswordResetToken.Create(user.Id, "hash:valid-token", DateTime.UtcNow.AddHours(1));
        fixture.PasswordResetTokens.AddExisting(token);
        var originalTokenVersion = user.TokenVersion;

        await fixture.UseCase.ExecuteAsync(new ConfirmPasswordResetInput
        {
            Token = "valid-token",
            NewPassword = "New_password_123!"
        });

        var updatedUser = await fixture.Users.FindByIdAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.True(fixture.PasswordHasher.Verify("New_password_123!", updatedUser!.PasswordHash));
        Assert.Equal(originalTokenVersion + 1, updatedUser.TokenVersion);
        Assert.Equal(1, fixture.RefreshTokens.RevokeActiveByUserIdCalls);
        Assert.True(token.IsConsumed);

        Assert.Contains(fixture.AuditLogs.Entries, entry => entry.Action == AuditLogActionNames.PasswordResetSucceeded);
        Assert.Contains(fixture.AuditLogs.Entries, entry => entry.Action == AuditLogActionNames.UserTokenVersionIncremented);
        Assert.Contains(fixture.AuditLogs.Entries, entry => entry.Action == AuditLogActionNames.UserSessionsRevoked);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenIsReused_ShouldThrowOnSecondAttempt()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User();
        fixture.Users.Add(user);
        var token = PasswordResetToken.Create(user.Id, "hash:valid-token", DateTime.UtcNow.AddHours(1));
        fixture.PasswordResetTokens.AddExisting(token);

        await fixture.UseCase.ExecuteAsync(new ConfirmPasswordResetInput
        {
            Token = "valid-token",
            NewPassword = "New_password_123!"
        });

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(new ConfirmPasswordResetInput
        {
            Token = "valid-token",
            NewPassword = "Another_password_456!"
        }));
    }

    private static ConfirmPasswordResetFixture CreateFixture()
    {
        var users = new FakeUserRepository();
        var passwordResetTokens = new FakePasswordResetTokenRepository();
        var passwordHasher = new FakePasswordHasher();
        var refreshTokens = new FakeRefreshTokenRepository();
        var unitOfWork = new FakeUnitOfWork();
        var auditLogs = new FakeAuditLogService();

        var useCase = new ConfirmPasswordResetUseCase(
            users,
            passwordResetTokens,
            new FakePasswordResetTokenHasher(),
            passwordHasher,
            new PasswordPolicy(),
            refreshTokens,
            unitOfWork,
            auditLogs);

        return new ConfirmPasswordResetFixture(useCase, users, passwordResetTokens, passwordHasher, refreshTokens, auditLogs);
    }

    private sealed record ConfirmPasswordResetFixture(
        ConfirmPasswordResetUseCase UseCase,
        FakeUserRepository Users,
        FakePasswordResetTokenRepository PasswordResetTokens,
        FakePasswordHasher PasswordHasher,
        FakeRefreshTokenRepository RefreshTokens,
        FakeAuditLogService AuditLogs);
}
