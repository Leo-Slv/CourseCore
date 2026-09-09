using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class RevokeUserAreaAccessUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenAccessExists_ShouldRevokePermissions()
    {
        var areas = new FakeAreaRepository();
        var users = new FakeUserRepository();
        var user = TestEntityFactory.User(email: "target@coursecore.local");
        var area = TestEntityFactory.Area();
        users.Add(user);
        areas.Areas.Add(area);
        var access = UserAreaAccess.Create(user.Id, area.Id, canView: true, canManage: true);
        areas.UserAreaAccesses.Add(access);
        var auditLogs = new FakeAuditLogService();
        var useCase = new RevokeUserAreaAccessUseCase(areas, users, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(user.Id, area.Id);

        Assert.False(access.CanView);
        Assert.False(access.CanManage);
        var entry = Assert.Single(auditLogs.Entries, entry => entry.Action == "UserAreaAccessRevoked");
        Assert.Equal($"target@coursecore.local → {area.Name}", entry.Metadata["displayName"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAccessDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new RevokeUserAreaAccessUseCase(
            new FakeAreaRepository(), new FakeUserRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid()));
    }
}
