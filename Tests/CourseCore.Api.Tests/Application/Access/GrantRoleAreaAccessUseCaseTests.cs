using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class GrantRoleAreaAccessUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenGrantExists_ShouldUpdateWithoutDuplicate()
    {
        var role = TestEntityFactory.Role();
        var area = TestEntityFactory.Area();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        roles.AddForUser(Guid.NewGuid(), role);
        areas.Areas.Add(area);
        areas.RoleAreaAccesses.Add(CourseCore.Api.Modules.Access.Domain.Entities.RoleAreaAccess.Create(role.Id, area.Id, true, false));
        var useCase = new GrantRoleAreaAccessUseCase(roles, areas, new FakeUnitOfWork(), new FakeAuditLogService());
        var output = await useCase.ExecuteAsync(new GrantRoleAreaAccessInput { RoleId = role.Id, AreaId = area.Id, CanView = false, CanManage = true });
        Assert.Single(areas.RoleAreaAccesses);
        Assert.False(output.CanView);
        Assert.True(output.CanManage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleIsInactive_ShouldThrowConflictWithoutCreatingGrant()
    {
        var role = TestEntityFactory.Role(active: false);
        var area = TestEntityFactory.Area();
        var roles = new FakeRoleRepository();
        var areas = new FakeAreaRepository();
        roles.AddForUser(Guid.NewGuid(), role);
        areas.Areas.Add(area);
        var useCase = new GrantRoleAreaAccessUseCase(
            roles,
            areas,
            new FakeUnitOfWork(),
            new FakeAuditLogService());

        await Assert.ThrowsAsync<ConflictException>(() => useCase.ExecuteAsync(new GrantRoleAreaAccessInput
        {
            RoleId = role.Id,
            AreaId = area.Id,
            CanView = true
        }));

        Assert.Empty(areas.RoleAreaAccesses);
    }
}
