using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class ListRoleAreaAccessUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnOnlyTheGivenRolesAreaAccesses()
    {
        var areas = new FakeAreaRepository();
        var roleId = Guid.NewGuid();
        var otherRoleId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        areas.RoleAreaAccesses.Add(RoleAreaAccess.Create(roleId, areaId, canView: true, canManage: true));
        areas.RoleAreaAccesses.Add(RoleAreaAccess.Create(otherRoleId, Guid.NewGuid(), canView: true, canManage: false));
        var useCase = new ListRoleAreaAccessUseCase(areas);

        var output = await useCase.ExecuteAsync(roleId);

        var result = Assert.Single(output);
        Assert.Equal(areaId, result.AreaId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleIdIsEmpty_ShouldThrow()
    {
        var useCase = new ListRoleAreaAccessUseCase(new FakeAreaRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(Guid.Empty));
    }
}
