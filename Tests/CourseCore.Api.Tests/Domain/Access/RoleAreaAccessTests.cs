using CourseCore.Api.Modules.Access.Domain.Entities;

namespace CourseCore.Api.Tests.Domain.Access;

public class RoleAreaAccessTests
{
    [Fact]
    public void Create_WhenPermissionExists_ShouldBeValid()
    {
        var access = RoleAreaAccess.Create(Guid.NewGuid(), Guid.NewGuid(), canView: true, canManage: false);

        Assert.True(access.CanView || access.CanManage);
    }

    [Fact]
    public void Revoke_WhenCalled_ShouldRemovePermissions()
    {
        var access = RoleAreaAccess.Create(Guid.NewGuid(), Guid.NewGuid(), canView: true, canManage: true);

        access.Revoke();

        Assert.False(access.CanView);
        Assert.False(access.CanManage);
    }
}
