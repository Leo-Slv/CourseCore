using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Application.Auth;

public class RefreshTokenUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenIsValid_ShouldReturnNewAccessTokenAndRefreshToken()
    {
        var fixture = CreateFixture();

        var output = await fixture.UseCase.ExecuteAsync("old-refresh-token");

        Assert.Equal("access-token-1", output.Token.AccessToken);
        Assert.Equal("new-refresh-token", output.Token.RefreshToken);
        Assert.Contains("Admin", output.Roles);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenIsValid_ShouldRevokeOldToken()
    {
        var fixture = CreateFixture();

        await fixture.UseCase.ExecuteAsync("old-refresh-token");

        Assert.True(fixture.ExistingRefreshToken.IsRevoked);
        Assert.Equal("hash:new-refresh-token", fixture.ExistingRefreshToken.ReplacedByTokenHash);
        Assert.Single(fixture.RefreshTokens.Updated);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenIsValid_ShouldPersistNewRefreshToken()
    {
        var fixture = CreateFixture();

        await fixture.UseCase.ExecuteAsync("old-refresh-token");

        var persistedToken = Assert.Single(fixture.RefreshTokens.Added);
        Assert.Equal("hash:new-refresh-token", persistedToken.TokenHash);
        Assert.Equal(fixture.UserId, persistedToken.UserId);
        Assert.Equal(1, fixture.UnitOfWork.ExecuteCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenIsValid_ShouldRecordRefreshTokenRotatedAuditLog()
    {
        var fixture = CreateFixture();

        await fixture.UseCase.ExecuteAsync("old-refresh-token");

        var auditLog = Assert.Single(fixture.AuditLogs.Entries);
        Assert.Equal(AuditLogActionNames.RefreshTokenRotated, auditLog.Action);
        Assert.Equal("RefreshToken", auditLog.EntityName);
        Assert.Equal(fixture.ExistingRefreshToken.Id, auditLog.EntityId);
        Assert.Equal(fixture.UserId, auditLog.UserId);
        Assert.Equal("rotated", auditLog.Metadata["result"]);
        Assert.DoesNotContain("token", string.Join(',', auditLog.Metadata.Keys), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenIsReused_ShouldThrowUnauthorizedAccessException()
    {
        var fixture = CreateFixture();
        await fixture.UseCase.ExecuteAsync("old-refresh-token");
        fixture.AuditLogs.Clear();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.UseCase.ExecuteAsync("old-refresh-token"));

        var auditLog = Assert.Single(fixture.AuditLogs.Entries);
        Assert.Equal(AuditLogActionNames.RefreshTokenRejected, auditLog.Action);
        Assert.Equal("invalid_or_inactive", auditLog.Metadata["reason"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenDoesNotExist_ShouldThrowUnauthorizedAccessException()
    {
        var fixture = CreateFixture(addRefreshToken: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.UseCase.ExecuteAsync("missing-token"));

        var auditLog = Assert.Single(fixture.AuditLogs.Entries);
        Assert.Equal(AuditLogActionNames.RefreshTokenRejected, auditLog.Action);
        Assert.Equal("invalid_or_inactive", auditLog.Metadata["reason"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenIsExpired_ShouldThrowUnauthorizedAccessException()
    {
        var fixture = CreateFixture(refreshTokenExpiresAt: DateTime.UtcNow.AddMinutes(-1));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.UseCase.ExecuteAsync("old-refresh-token"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenIsRevoked_ShouldThrowUnauthorizedAccessException()
    {
        var fixture = CreateFixture(revokedAt: DateTime.UtcNow);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.UseCase.ExecuteAsync("old-refresh-token"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsInactive_ShouldThrowUnauthorizedAccessException()
    {
        var fixture = CreateFixture(userActive: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.UseCase.ExecuteAsync("old-refresh-token"));
    }

    private static RefreshTokenFixture CreateFixture(
        bool addRefreshToken = true,
        bool userActive = true,
        DateTime? refreshTokenExpiresAt = null,
        DateTime? revokedAt = null)
    {
        var userId = Guid.NewGuid();
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var refreshTokens = new FakeRefreshTokenRepository();
        var unitOfWork = new FakeUnitOfWork();
        var auditLogs = new FakeAuditLogService();
        var user = TestEntityFactory.User(userId, active: userActive);
        users.Add(user);
        roles.AddForUser(user.Id, TestEntityFactory.Role(name: "Admin"));

        var existingRefreshToken = RefreshToken.Restore(
            Guid.NewGuid(),
            user.Id,
            "hash:old-refresh-token",
            refreshTokenExpiresAt ?? DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(-1),
            revokedAt,
            replacedByTokenHash: null);

        if (addRefreshToken)
        {
            refreshTokens.AddExisting(existingRefreshToken);
        }

        var useCase = new RefreshTokenUseCase(
            users,
            roles,
            new FakeTokenService(),
            refreshTokens,
            new FakeRefreshTokenHasher(),
            new FakeRefreshTokenGenerator("new-refresh-token"),
            unitOfWork,
            auditLogs,
            Options.Create(new JwtOptions
            {
                AccessTokenExpirationMinutes = 60,
                RefreshTokenExpirationDays = 7
            }),
            NullLogger<RefreshTokenUseCase>.Instance);

        return new RefreshTokenFixture(useCase, refreshTokens, unitOfWork, existingRefreshToken, user.Id, auditLogs);
    }

    private sealed record RefreshTokenFixture(
        RefreshTokenUseCase UseCase,
        FakeRefreshTokenRepository RefreshTokens,
        FakeUnitOfWork UnitOfWork,
        RefreshToken ExistingRefreshToken,
        Guid UserId,
        FakeAuditLogService AuditLogs);
}
