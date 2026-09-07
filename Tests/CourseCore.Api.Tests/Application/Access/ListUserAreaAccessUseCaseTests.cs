using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class ListUserAreaAccessUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnUsersAreaAccesses()
    {
        var areas = new FakeAreaRepository();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var areaId = Guid.NewGuid();
        areas.UserAreaAccesses.Add(UserAreaAccess.Create(userId, areaId, canView: true, canManage: false));
        areas.UserAreaAccesses.Add(UserAreaAccess.Create(otherUserId, Guid.NewGuid(), canView: true, canManage: false));
        var useCase = new ListUserAreaAccessUseCase(areas);

        var output = await useCase.ExecuteAsync(userId);

        var result = Assert.Single(output);
        Assert.Equal(areaId, result.AreaId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIdIsEmpty_ShouldThrow()
    {
        var useCase = new ListUserAreaAccessUseCase(new FakeAreaRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(Guid.Empty));
    }
}
