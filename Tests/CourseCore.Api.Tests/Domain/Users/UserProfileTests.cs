using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Domain.Users;

public class UserProfileTests
{
    [Fact]
    public void ChangePhone_ShouldSetTrimmedValueAndUpdateTimestamp()
    {
        var user = TestEntityFactory.User();
        var previousUpdatedAt = user.UpdatedAt;

        user.ChangePhone("  +55 11 99999-0000  ");

        Assert.Equal("+55 11 99999-0000", user.Phone);
        Assert.True(user.UpdatedAt >= previousUpdatedAt);
    }

    [Fact]
    public void ChangePhone_WhenNullOrWhitespace_ShouldClearValue()
    {
        var user = TestEntityFactory.User();
        user.ChangePhone("+55 11 99999-0000");

        user.ChangePhone("   ");

        Assert.Null(user.Phone);
    }

    [Fact]
    public void ChangeAvatarUrl_ShouldSetTrimmedValueAndUpdateTimestamp()
    {
        var user = TestEntityFactory.User();
        var previousUpdatedAt = user.UpdatedAt;

        user.ChangeAvatarUrl("  https://cdn.coursecore.local/avatar.png  ");

        Assert.Equal("https://cdn.coursecore.local/avatar.png", user.AvatarUrl);
        Assert.True(user.UpdatedAt >= previousUpdatedAt);
    }

    [Fact]
    public void ChangeAvatarUrl_WhenNullOrWhitespace_ShouldClearValue()
    {
        var user = TestEntityFactory.User();
        user.ChangeAvatarUrl("https://cdn.coursecore.local/avatar.png");

        user.ChangeAvatarUrl(null);

        Assert.Null(user.AvatarUrl);
    }
}
