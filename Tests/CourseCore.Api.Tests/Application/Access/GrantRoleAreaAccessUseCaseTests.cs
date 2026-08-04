using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class GrantRoleAreaAccessUseCaseTests
{
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
