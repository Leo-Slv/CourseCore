using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Domain.Users;

public class UserTokenVersionTests
{
    [Fact]
    public void IncrementTokenVersion_ShouldIncreaseTokenVersionAndUpdateTimestamp()
    {
        var user = TestEntityFactory.User(tokenVersion: 2);
        var previousUpdatedAt = user.UpdatedAt;

        user.IncrementTokenVersion();

        Assert.Equal(3, user.TokenVersion);
        Assert.True(user.UpdatedAt >= previousUpdatedAt);
    }
}
