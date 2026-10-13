using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Modules.Auth.Application.Services;
using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Application.Auth;

public class LoginUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenLoginIsValid_ShouldReturnAccessTokenAndRefreshToken()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(new LoginInput
        {
            Email = "user@coursecore.local",
            Password = "password"
        });

        Assert.Equal("access-token-1", output.Token.AccessToken);
        Assert.Equal("refresh-token", output.Token.RefreshToken);
        Assert.Contains("Admin", output.Roles);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLoginIsValid_ShouldPersistRefreshTokenHashOnly()
    {
        var fixture = CreateFixture();

        await fixture.UseCase.ExecuteAsync(new LoginInput
        {
            Email = "user@coursecore.local",
            Password = "password"
        });

        var storedToken = Assert.Single(fixture.RefreshTokens.Added);
        Assert.Equal("hash:refresh-token", storedToken.TokenHash);
        Assert.NotEqual("refresh-token", storedToken.TokenHash);
        Assert.Equal(1, fixture.UnitOfWork.ExecuteCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLoginIsValid_ShouldRecordLoginSucceededAuditLog()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync(new LoginInput
        {
            Email = "user@coursecore.local",
            Password = "password"
        });

        var auditLog = Assert.Single(fixture.AuditLogs.Entries);
        Assert.Equal(AuditLogActionNames.LoginSucceeded, auditLog.Action);
        Assert.Equal("User", auditLog.EntityName);
        Assert.Equal(output.UserId, auditLog.EntityId);
        Assert.Equal(output.UserId, auditLog.UserId);
        Assert.Equal("succeeded", auditLog.Metadata["result"]);
        Assert.DoesNotContain("token", string.Join(',', auditLog.Metadata.Keys), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordIsInvalid_ShouldThrowUnauthorizedAccessException()
    {
        var fixture = CreateFixture();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.UseCase.ExecuteAsync(new LoginInput
        {
            Email = "user@coursecore.local",
            Password = "wrong"
        }));

        Assert.Equal("Invalid credentials.", exception.Message);
        Assert.Equal(1, fixture.PasswordHasher.VerifyCalls);
        Assert.Equal(0, fixture.PasswordHasher.VerifyDummyCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowUnauthorizedAccessException()
    {
        var fixture = CreateFixture(addUser: false);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.UseCase.ExecuteAsync(new LoginInput
        {
            Email = "missing@coursecore.local",
            Password = "password"
        }));

        Assert.Equal("Invalid credentials.", exception.Message);
        Assert.Equal(0, fixture.PasswordHasher.VerifyCalls);
        Assert.Equal(1, fixture.PasswordHasher.VerifyDummyCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsInactive_ShouldThrowUnauthorizedAccessException()
    {
        var fixture = CreateFixture(userActive: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.UseCase.ExecuteAsync(new LoginInput
        {
            Email = "user@coursecore.local",
            Password = "password"
        }));
    }

    private static LoginInput Attempt(string password, string email = "user@coursecore.local")
    {
        return new LoginInput { Email = email, Password = password };
    }

    private static async Task FailRepeatedlyAsync(LoginFixture fixture, int times, string email = "user@coursecore.local")
    {
        for (var i = 0; i < times; i++)
        {
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => fixture.UseCase.ExecuteAsync(Attempt("wrong", email)));
        }
    }

    [Fact]
    public async Task ExecuteAsync_AfterFiveFailures_ShouldRejectEvenTheCorrectPasswordWithRetryAfter()
    {
        var fixture = CreateFixture();
        await FailRepeatedlyAsync(fixture, 5);

        var exception = await Assert.ThrowsAsync<TooManyRequestsException>(
            () => fixture.UseCase.ExecuteAsync(Attempt("password")));

        Assert.True(exception.RetryAfter > TimeSpan.Zero);
        Assert.True(exception.RetryAfter <= TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task ExecuteAsync_WhenLocked_ShouldNotEvenCheckThePassword()
    {
        var fixture = CreateFixture();
        await FailRepeatedlyAsync(fixture, 5);
        var checksBefore = fixture.PasswordHasher.VerifyCalls + fixture.PasswordHasher.VerifyDummyCalls;

        await Assert.ThrowsAsync<TooManyRequestsException>(() => fixture.UseCase.ExecuteAsync(Attempt("password")));

        Assert.Equal(checksBefore, fixture.PasswordHasher.VerifyCalls + fixture.PasswordHasher.VerifyDummyCalls);
    }

    [Fact]
    public async Task ExecuteAsync_AfterTheLockoutExpires_ShouldAcceptTheCorrectPasswordAgain()
    {
        var fixture = CreateFixture();
        await FailRepeatedlyAsync(fixture, 5);

        fixture.Time.Advance(TimeSpan.FromMinutes(16));
        var output = await fixture.UseCase.ExecuteAsync(Attempt("password"));

        Assert.Equal("access-token-1", output.Token.AccessToken);
    }

    [Fact]
    public async Task ExecuteAsync_AFewFailuresThenSuccess_ShouldResetTheCounter()
    {
        var fixture = CreateFixture();
        await FailRepeatedlyAsync(fixture, 4);
        await fixture.UseCase.ExecuteAsync(Attempt("password"));

        // Four more failures would have locked the account without the reset (4 + 4 > 5).
        await FailRepeatedlyAsync(fixture, 4);
        var output = await fixture.UseCase.ExecuteAsync(Attempt("password"));

        Assert.NotNull(output);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownEmails_AreThrottledToo_SoLockoutDoesNotRevealWhichAccountsExist()
    {
        var fixture = CreateFixture();
        await FailRepeatedlyAsync(fixture, 5, "nobody@coursecore.local");

        await Assert.ThrowsAsync<TooManyRequestsException>(
            () => fixture.UseCase.ExecuteAsync(Attempt("anything", "nobody@coursecore.local")));
    }

    [Fact]
    public async Task ExecuteAsync_LockingOneAccount_ShouldNotAffectAnotherOne()
    {
        var fixture = CreateFixture();
        await FailRepeatedlyAsync(fixture, 5, "other@coursecore.local");

        var output = await fixture.UseCase.ExecuteAsync(Attempt("password"));

        Assert.NotNull(output);
    }

    private static LoginFixture CreateFixture(bool addUser = true, bool userActive = true)
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var refreshTokens = new FakeRefreshTokenRepository();
        var unitOfWork = new FakeUnitOfWork();
        var auditLogs = new FakeAuditLogService();
        var user = TestEntityFactory.User(active: userActive);

        if (addUser)
        {
            users.Add(user);
            roles.AddForUser(user.Id, TestEntityFactory.Role(name: "Admin"));
        }

        var passwordHasher = new FakePasswordHasher();
        var time = new ManualTimeProvider();
        var tracker = new InMemoryLoginAttemptTracker(Options.Create(new LoginLockoutOptions()), time);
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
        var useCase = new LoginUseCase(
            users,
            passwordHasher,
            refreshTokens,
            sessionIssuer,
            unitOfWork,
            auditLogs,
            tracker,
            NullLogger<LoginUseCase>.Instance);

        return new LoginFixture(useCase, refreshTokens, unitOfWork, auditLogs, passwordHasher, tracker, time);
    }

    private sealed record LoginFixture(
        LoginUseCase UseCase,
        FakeRefreshTokenRepository RefreshTokens,
        FakeUnitOfWork UnitOfWork,
        FakeAuditLogService AuditLogs,
        FakePasswordHasher PasswordHasher,
        InMemoryLoginAttemptTracker Tracker,
        ManualTimeProvider Time);
}
