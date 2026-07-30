using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace CourseCore.Api.Tests.Application.Auth;

public class LogoutUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenIsValid_ShouldRevokeTokenAndAuditLogout()
    {
        var fixture = CreateFixture();

        await fixture.UseCase.ExecuteAsync("refresh-token");

        Assert.True(fixture.RefreshToken.IsRevoked);
        var auditLog = Assert.Single(fixture.AuditLogs.Entries);
        Assert.Equal(AuditLogActionNames.LogoutSucceeded, auditLog.Action);
        Assert.Equal(fixture.RefreshToken.Id, auditLog.EntityId);
        Assert.Equal(fixture.UserId, auditLog.UserId);
        Assert.DoesNotContain("token", string.Join(',', auditLog.Metadata.Keys), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", string.Join(',', auditLog.Metadata.Keys), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenIsAlreadyRevoked_ShouldCompleteWithoutAuditDetails()
    {
        var fixture = CreateFixture(revokedAt: DateTime.UtcNow);

        await fixture.UseCase.ExecuteAsync("refresh-token");

        Assert.Empty(fixture.AuditLogs.Entries);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenDoesNotExist_ShouldCompleteWithoutAuditDetails()
    {
        var fixture = CreateFixture(addRefreshToken: false);

        await fixture.UseCase.ExecuteAsync("missing-token");

        Assert.Empty(fixture.AuditLogs.Entries);
    }

    private static LogoutFixture CreateFixture(
        bool addRefreshToken = true,
        DateTime? revokedAt = null)
    {
        var userId = Guid.NewGuid();
        var refreshTokens = new FakeRefreshTokenRepository();
        var unitOfWork = new FakeUnitOfWork();
        var auditLogs = new FakeAuditLogService();
        var refreshToken = RefreshToken.Restore(
            Guid.NewGuid(),
            userId,
            "hash:refresh-token",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(-1),
            revokedAt,
            replacedByTokenHash: null);

        if (addRefreshToken)
        {
            refreshTokens.AddExisting(refreshToken);
        }

        var useCase = new LogoutUseCase(
            refreshTokens,
            new FakeRefreshTokenHasher(),
            unitOfWork,
            auditLogs,
            NullLogger<LogoutUseCase>.Instance);

        return new LogoutFixture(useCase, refreshToken, userId, auditLogs);
    }

    private sealed record LogoutFixture(
        LogoutUseCase UseCase,
        RefreshToken RefreshToken,
        Guid UserId,
        FakeAuditLogService AuditLogs);
}
