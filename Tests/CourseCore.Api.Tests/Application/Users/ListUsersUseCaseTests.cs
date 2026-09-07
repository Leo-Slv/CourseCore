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
        var useCase = new ListUsersUseCase(users, new FakeRoleRepository());

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
        var useCase = new ListUsersUseCase(users, new FakeRoleRepository());

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
        var useCase = new ListUsersUseCase(users, roles);

        var output = await useCase.ExecuteAsync(new ListUsersInput());

        var item = Assert.Single(output.Page.Items);
        Assert.Contains("Admin", item.RoleNames);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPageIsInvalid_ShouldThrow()
    {
        var useCase = new ListUsersUseCase(new FakeUserRepository(), new FakeRoleRepository());

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => useCase.ExecuteAsync(new ListUsersInput { Page = 0 }));
    }
}
