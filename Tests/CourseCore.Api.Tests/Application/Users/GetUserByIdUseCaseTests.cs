using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Users.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Users;

public class GetUserByIdUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldReturnUserWithRoleNames()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var user = TestEntityFactory.User();
        users.Add(user);
        var role = TestEntityFactory.Role(name: "Admin");
        roles.AddForUser(user.Id, role);
        var useCase = new GetUserByIdUseCase(users, roles, new FakeAreaRepository());

        var output = await useCase.ExecuteAsync(user.Id);

        Assert.Equal(user.Id, output.Id);
        Assert.Contains("Admin", output.RoleNames);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasAreaGrant_ShouldReturnAreaNames()
    {
        var users = new FakeUserRepository();
        var areas = new FakeAreaRepository();
        var user = TestEntityFactory.User();
        users.Add(user);
        var area = TestEntityFactory.Area();
        areas.Areas.Add(area);
        areas.UserAreaAccesses.Add(UserAreaAccess.Create(user.Id, area.Id, canView: true, canManage: false));
        var useCase = new GetUserByIdUseCase(users, new FakeRoleRepository(), areas);

        var output = await useCase.ExecuteAsync(user.Id);

        Assert.Contains(area.Name, output.AreaNames);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        var useCase = new GetUserByIdUseCase(new FakeUserRepository(), new FakeRoleRepository(), new FakeAreaRepository());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid()));
    }
}
