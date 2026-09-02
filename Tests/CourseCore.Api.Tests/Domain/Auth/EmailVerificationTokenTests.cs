using CourseCore.Api.Modules.Auth.Domain.Entities;

namespace CourseCore.Api.Tests.Domain.Auth;

public class EmailVerificationTokenTests
{
    [Fact]
    public void Create_WhenTokenIsNotExpired_ShouldBeActive()
    {
        var token = EmailVerificationToken.Create(
            Guid.NewGuid(),
            "token-hash",
            DateTime.UtcNow.AddHours(1));

        Assert.True(token.IsActive);
        Assert.False(token.IsExpired);
        Assert.False(token.IsConsumed);
    }

    [Fact]
    public void Restore_WhenTokenIsExpired_ShouldNotBeActive()
    {
        var token = EmailVerificationToken.Restore(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token-hash",
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddHours(-2),
            consumedAt: null);

        Assert.True(token.IsExpired);
        Assert.False(token.IsActive);
    }

    [Fact]
    public void Restore_WhenTokenIsConsumed_ShouldNotBeActive()
    {
        var token = EmailVerificationToken.Restore(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "token-hash",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow);

        Assert.True(token.IsConsumed);
        Assert.False(token.IsActive);
    }

    [Fact]
    public void Consume_WhenTokenIsActive_ShouldFillConsumedAt()
    {
        var consumedAt = DateTime.UtcNow;
        var token = EmailVerificationToken.Create(
            Guid.NewGuid(),
            "token-hash",
            DateTime.UtcNow.AddHours(1));

        token.Consume(consumedAt);

        Assert.Equal(consumedAt, token.ConsumedAt);
        Assert.True(token.IsConsumed);
        Assert.False(token.IsActive);
    }

    [Fact]
    public void Consume_WhenTokenIsAlreadyConsumed_ShouldNotOverwriteConsumedAt()
    {
        var firstConsumedAt = DateTime.UtcNow.AddMinutes(-5);
        var token = EmailVerificationToken.Create(
            Guid.NewGuid(),
            "token-hash",
            DateTime.UtcNow.AddHours(1));

        token.Consume(firstConsumedAt);
        token.Consume(DateTime.UtcNow);

        Assert.Equal(firstConsumedAt, token.ConsumedAt);
    }
}
