using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Domain.Users;

public class UserEmailVerificationTests
{
    [Fact]
    public void MarkEmailAsVerified_WhenNotYetVerified_ShouldFillEmailVerifiedAt()
    {
        var user = TestEntityFactory.User(emailVerified: false);
        var verifiedAt = DateTime.UtcNow;

        user.MarkEmailAsVerified(verifiedAt);

        Assert.Equal(verifiedAt, user.EmailVerifiedAt);
    }

    [Fact]
    public void MarkEmailAsVerified_WhenTimestampIsOmitted_ShouldDefaultToUtcNow()
    {
        var user = TestEntityFactory.User(emailVerified: false);
        var before = DateTime.UtcNow;

        user.MarkEmailAsVerified();

        Assert.NotNull(user.EmailVerifiedAt);
        Assert.True(user.EmailVerifiedAt >= before);
    }
}
