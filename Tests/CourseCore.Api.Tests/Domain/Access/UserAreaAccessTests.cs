using CourseCore.Api.Modules.Access.Domain.Entities;

namespace CourseCore.Api.Tests.Domain.Access;

public class UserAreaAccessTests
{
    [Fact]
    public void IsValidAt_WhenDateIsInsidePeriodAndPermissionExists_ShouldBeValid()
    {
        var now = DateTime.UtcNow;
        var access = UserAreaAccess.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            canView: true,
            canManage: false,
            now.AddDays(-1),
            now.AddDays(1));

        Assert.True(access.IsValidAt(now));
    }

    [Fact]
    public void IsValidAt_WhenDateIsOutsidePeriod_ShouldBeInvalid()
    {
        var now = DateTime.UtcNow;
        var access = UserAreaAccess.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            canView: true,
            canManage: false,
            now.AddDays(1),
            now.AddDays(2));

        Assert.False(access.IsValidAt(now));
    }

    [Fact]
    public void IsValidAt_WhenAccessIsRevoked_ShouldBeInvalid()
    {
        var access = UserAreaAccess.Create(Guid.NewGuid(), Guid.NewGuid(), canView: true, canManage: false);

        access.Revoke();

        Assert.False(access.IsValidAt(DateTime.UtcNow));
    }

    [Fact]
    public void Revoke_WhenCalled_ShouldRemovePermissions()
    {
        var access = UserAreaAccess.Create(Guid.NewGuid(), Guid.NewGuid(), canView: true, canManage: true);

        access.Revoke();

        Assert.False(access.CanView);
        Assert.False(access.CanManage);
    }
}
