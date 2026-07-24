using CourseCore.Api.Modules.Auth.Application.DTOs;
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

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.UseCase.ExecuteAsync(new LoginInput
        {
            Email = "user@coursecore.local",
            Password = "wrong"
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowUnauthorizedAccessException()
    {
        var fixture = CreateFixture(addUser: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.UseCase.ExecuteAsync(new LoginInput
        {
            Email = "missing@coursecore.local",
            Password = "password"
        }));
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

        var useCase = new LoginUseCase(
            users,
            roles,
            new FakePasswordHasher(),
            new FakeTokenService(),
            refreshTokens,
            new FakeRefreshTokenHasher(),
            new FakeRefreshTokenGenerator("refresh-token"),
            unitOfWork,
            auditLogs,
            Options.Create(new JwtOptions
            {
                AccessTokenExpirationMinutes = 60,
                RefreshTokenExpirationDays = 7
            }),
            NullLogger<LoginUseCase>.Instance);

        return new LoginFixture(useCase, refreshTokens, unitOfWork, auditLogs);
    }

    private sealed record LoginFixture(
        LoginUseCase UseCase,
        FakeRefreshTokenRepository RefreshTokens,
        FakeUnitOfWork UnitOfWork,
        FakeAuditLogService AuditLogs);
}
