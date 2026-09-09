using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Application.Services;
using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Auth;

public class ChangeOwnPasswordUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCurrentPasswordIsCorrect_ShouldChangePasswordAndRevokeSessions()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(passwordHash: "hashed:Current_password_123!");
        fixture.Users.Add(user);
        fixture.RefreshTokens.AddExisting(RefreshToken.Create(
            user.Id,
            "hash:refresh-token",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddMinutes(-10)));
        var originalTokenVersion = user.TokenVersion;

        await fixture.UseCase.ExecuteAsync(new ChangeOwnPasswordInput
        {
            UserId = user.Id,
            CurrentPassword = "Current_password_123!",
            NewPassword = "New_password_456!"
        });

        Assert.True(fixture.PasswordHasher.Verify("New_password_456!", user.PasswordHash));
        Assert.Equal(originalTokenVersion + 1, user.TokenVersion);
        Assert.Equal(1, fixture.RefreshTokens.RevokeActiveByUserIdCalls);
        Assert.Contains(fixture.AuditLogs.Entries, entry => entry.Action == AuditLogActionNames.PasswordChanged);
        Assert.Contains(fixture.AuditLogs.Entries, entry => entry.Action == AuditLogActionNames.UserTokenVersionIncremented);
        Assert.Contains(fixture.AuditLogs.Entries, entry => entry.Action == AuditLogActionNames.UserSessionsRevoked);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCurrentPasswordIsWrong_ShouldThrowUnauthorized()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(passwordHash: "hashed:Current_password_123!");
        fixture.Users.Add(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.UseCase.ExecuteAsync(new ChangeOwnPasswordInput
        {
            UserId = user.Id,
            CurrentPassword = "Wrong_password_000!",
            NewPassword = "New_password_456!"
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNewPasswordIsWeak_ShouldThrow()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User(passwordHash: "hashed:Current_password_123!");
        fixture.Users.Add(user);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(new ChangeOwnPasswordInput
        {
            UserId = user.Id,
            CurrentPassword = "Current_password_123!",
            NewPassword = "weak"
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldThrow()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(new ChangeOwnPasswordInput
        {
            UserId = Guid.NewGuid(),
            CurrentPassword = "Current_password_123!",
            NewPassword = "New_password_456!"
        }));
    }

    private static ChangeOwnPasswordFixture CreateFixture()
    {
        var users = new FakeUserRepository();
        var passwordHasher = new FakePasswordHasher();
        var refreshTokens = new FakeRefreshTokenRepository();
        var auditLogs = new FakeAuditLogService();

        var useCase = new ChangeOwnPasswordUseCase(
            users,
            passwordHasher,
            new PasswordPolicy(),
            refreshTokens,
            new FakeUnitOfWork(),
            auditLogs);

        return new ChangeOwnPasswordFixture(useCase, users, passwordHasher, refreshTokens, auditLogs);
    }

    private sealed record ChangeOwnPasswordFixture(
        ChangeOwnPasswordUseCase UseCase,
        FakeUserRepository Users,
        FakePasswordHasher PasswordHasher,
        FakeRefreshTokenRepository RefreshTokens,
        FakeAuditLogService AuditLogs);
}
