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
        var userId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        var access = UserAreaAccess.Create(userId, areaId, canView: true, canManage: true);
        areas.UserAreaAccesses.Add(access);
        var auditLogs = new FakeAuditLogService();
        var useCase = new RevokeUserAreaAccessUseCase(areas, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(userId, areaId);

        Assert.False(access.CanView);
        Assert.False(access.CanManage);
        Assert.Contains(auditLogs.Entries, entry => entry.Action == "UserAreaAccessRevoked");
    }

    [Fact]
    public async Task ExecuteAsync_WhenAccessDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new RevokeUserAreaAccessUseCase(new FakeAreaRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid()));
    }
}
