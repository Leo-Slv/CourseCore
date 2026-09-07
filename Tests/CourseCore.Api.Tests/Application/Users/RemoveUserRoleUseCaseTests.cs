using CourseCore.Api.Modules.Users.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Users;

public class RemoveUserRoleUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenRoleIsAssigned_ShouldRemoveRole()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var user = TestEntityFactory.User();
        var role = TestEntityFactory.Role();
        users.Add(user);
        await roles.CreateAsync(role);
        roles.AddForUser(user.Id, role);
        var auditLogs = new FakeAuditLogService();
        var useCase = new RemoveUserRoleUseCase(users, roles, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(user.Id, role.Id);

        var remainingRoles = await roles.FindByUserIdAsync(user.Id);
        Assert.DoesNotContain(remainingRoles, r => r.Id == role.Id);
        Assert.Contains(auditLogs.Entries, entry => entry.Action == "UserRoleUnassigned");
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        var roles = new FakeRoleRepository();
        var role = TestEntityFactory.Role();
        await roles.CreateAsync(role);
        var useCase = new RemoveUserRoleUseCase(new FakeUserRepository(), roles, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid(), role.Id));
    }
}
