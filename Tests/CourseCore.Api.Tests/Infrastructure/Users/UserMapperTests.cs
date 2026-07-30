using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Infrastructure.Users;

public class UserMapperTests
{
    [Fact]
    public void ToPersistence_ShouldPreserveTokenVersion()
    {
        var user = TestEntityFactory.User(tokenVersion: 4);

        var model = UserMapper.ToPersistence(user);

        Assert.Equal(4, model.TokenVersion);
    }

    [Fact]
    public void ToDomain_ShouldPreserveTokenVersion()
    {
        var user = TestEntityFactory.User(tokenVersion: 5);
        var model = UserMapper.ToPersistence(user);

        var restored = UserMapper.ToDomain(model);

        Assert.Equal(5, restored.TokenVersion);
    }

    [Fact]
    public void ApplyChanges_ShouldPreserveTokenVersion()
    {
        var user = TestEntityFactory.User(tokenVersion: 6);
        var model = UserMapper.ToPersistence(TestEntityFactory.User(tokenVersion: 1));

        UserMapper.ApplyChanges(user, model);

        Assert.Equal(6, model.TokenVersion);
    }
}
