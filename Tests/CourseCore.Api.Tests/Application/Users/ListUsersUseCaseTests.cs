using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Users.Application.DTOs;
using CourseCore.Api.Modules.Users.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Users;

public class ListUsersUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnTotalRegisteredAndTotalConfirmed()
    {
        var users = new FakeUserRepository();
        users.Add(TestEntityFactory.User(email: "confirmed@coursecore.local", emailVerified: true));
        users.Add(TestEntityFactory.User(email: "pending@coursecore.local", emailVerified: false));
        var useCase = new ListUsersUseCase(users, new FakeRoleRepository(), new FakeAreaRepository());

        var output = await useCase.ExecuteAsync(new ListUsersInput());

        Assert.Equal(2, output.TotalRegistered);
        Assert.Equal(1, output.TotalConfirmed);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSearchMatchesName_ShouldFilterResults()
    {
        var users = new FakeUserRepository();
        var target = TestEntityFactory.User(email: "target@coursecore.local");
        users.Add(target);
        users.Add(TestEntityFactory.User(email: "other@coursecore.local"));
        var useCase = new ListUsersUseCase(users, new FakeRoleRepository(), new FakeAreaRepository());

        var output = await useCase.ExecuteAsync(new ListUsersInput { Search = "target" });

        var item = Assert.Single(output.Page.Items);
        Assert.Equal(target.Id, item.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncludeRoleNamesPerUser()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var user = TestEntityFactory.User();
        users.Add(user);
        var role = TestEntityFactory.Role(name: "Admin");
        roles.AddForUser(user.Id, role);
        var useCase = new ListUsersUseCase(users, roles, new FakeAreaRepository());

        var output = await useCase.ExecuteAsync(new ListUsersInput());

        var item = Assert.Single(output.Page.Items);
        Assert.Contains("Admin", item.RoleNames);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncludeGrantedAreaNamesPerUser()
    {
        var users = new FakeUserRepository();
        var areas = new FakeAreaRepository();
        var user = TestEntityFactory.User();
        users.Add(user);
        var grantedArea = TestEntityFactory.Area();
        var revokedArea = TestEntityFactory.Area();
        areas.Areas.Add(grantedArea);
        areas.Areas.Add(revokedArea);
        areas.UserAreaAccesses.Add(UserAreaAccess.Create(user.Id, grantedArea.Id, canView: true, canManage: false));
        var revokedAccess = UserAreaAccess.Create(user.Id, revokedArea.Id, canView: true, canManage: false);
        revokedAccess.Revoke();
        areas.UserAreaAccesses.Add(revokedAccess);
        var useCase = new ListUsersUseCase(users, new FakeRoleRepository(), areas);

        var output = await useCase.ExecuteAsync(new ListUsersInput());

        var item = Assert.Single(output.Page.Items);
        var areaName = Assert.Single(item.AreaNames);
        Assert.Equal(grantedArea.Name, areaName);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPageIsInvalid_ShouldThrow()
    {
        var useCase = new ListUsersUseCase(new FakeUserRepository(), new FakeRoleRepository(), new FakeAreaRepository());

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => useCase.ExecuteAsync(new ListUsersInput { Page = 0 }));
    }
}
