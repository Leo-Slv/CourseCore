using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Auth;

public class GetCurrentUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldReturnProfileWithRoles()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var user = TestEntityFactory.User(email: "member@coursecore.local", emailVerified: true);
        var role = TestEntityFactory.Role(name: "Admin");
        users.Add(user);
        roles.AddForUser(user.Id, role);
        var useCase = new GetCurrentUserUseCase(users, roles);

        var output = await useCase.ExecuteAsync(user.Id);

        Assert.Equal(user.Id, output.UserId);
        Assert.Equal(user.Name, output.Name);
        Assert.Equal("member@coursecore.local", output.Email);
        Assert.True(output.Active);
        Assert.NotNull(output.EmailVerifiedAt);
        Assert.Equal("Admin", Assert.Single(output.Roles));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserHasNoRoles_ShouldReturnEmptyRoles()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var user = TestEntityFactory.User();
        users.Add(user);
        var useCase = new GetCurrentUserUseCase(users, roles);

        var output = await useCase.ExecuteAsync(user.Id);

        Assert.Empty(output.Roles);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowNotFound()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var useCase = new GetCurrentUserUseCase(users, roles);

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid()));
    }
}
