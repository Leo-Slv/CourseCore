using CourseCore.Api.Modules.Auth.Domain.Entities;

namespace CourseCore.Api.Tests.Domain.Auth;

public class RefreshTokenTests
{
    [Fact]
    public void Create_WhenTokenIsNotExpired_ShouldBeActive()
    {
        var refreshToken = RefreshToken.Create(
            Guid.NewGuid(),
            "token-hash",
            DateTime.UtcNow.AddDays(1));

        Assert.True(refreshToken.IsActive);
        Assert.False(refreshToken.IsExpired);
        Assert.False(refreshToken.IsRevoked);
    }

    [Fact]
    public void Restore_WhenTokenIsExpired_ShouldNotBeActive()
    {
        var refreshToken = RefreshToken.Restore(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token-hash",
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddDays(-2),
            revokedAt: null,
            replacedByTokenHash: null);

        Assert.True(refreshToken.IsExpired);
        Assert.False(refreshToken.IsActive);
    }

    [Fact]
    public void Restore_WhenTokenIsRevoked_ShouldNotBeActive()
    {
        var refreshToken = RefreshToken.Restore(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token-hash",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            replacedByTokenHash: null);

        Assert.True(refreshToken.IsRevoked);
        Assert.False(refreshToken.IsActive);
    }

    [Fact]
    public void Revoke_WhenTokenIsActive_ShouldFillRevokedAt()
    {
        var revokedAt = DateTime.UtcNow;
        var refreshToken = RefreshToken.Create(
            Guid.NewGuid(),
            "token-hash",
            DateTime.UtcNow.AddDays(1));

        refreshToken.Revoke(revokedAt: revokedAt);

        Assert.Equal(revokedAt, refreshToken.RevokedAt);
        Assert.True(refreshToken.IsRevoked);
    }

    [Fact]
    public void Revoke_WhenReplacementHashIsProvided_ShouldFillReplacedByTokenHash()
    {
        var refreshToken = RefreshToken.Create(
            Guid.NewGuid(),
            "token-hash",
            DateTime.UtcNow.AddDays(1));

        refreshToken.Revoke("replacement-hash");

        Assert.Equal("replacement-hash", refreshToken.ReplacedByTokenHash);
    }

    [Fact]
    public void Revoke_WhenTokenIsRevoked_ShouldNotAllowActiveState()
    {
        var refreshToken = RefreshToken.Create(
            Guid.NewGuid(),
            "token-hash",
            DateTime.UtcNow.AddDays(1));

        refreshToken.Revoke();

        Assert.False(refreshToken.IsActive);
    }
}
