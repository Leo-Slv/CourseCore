using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class GrantUserAreaAccessUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenGrantExists_ShouldUpdateWithoutDuplicate()
    {
        var user = TestEntityFactory.User();
        var area = TestEntityFactory.Area();
        var users = new FakeUserRepository();
        var areas = new FakeAreaRepository();
        users.Add(user);
        areas.Areas.Add(area);
        areas.UserAreaAccesses.Add(UserAreaAccess.Create(user.Id, area.Id, true, false));
        var useCase = new GrantUserAreaAccessUseCase(users, areas, new FakeUnitOfWork(), new FakeAuditLogService());
        var output = await useCase.ExecuteAsync(new GrantUserAreaAccessInput { UserId = user.Id, AreaId = area.Id, CanView = false, CanManage = true, StartsAt = DateTime.UtcNow.AddMinutes(-1), ExpiresAt = DateTime.UtcNow.AddDays(1) });
        Assert.Single(areas.UserAreaAccesses);
        Assert.False(output.CanView);
        Assert.True(output.CanManage);
    }
}
