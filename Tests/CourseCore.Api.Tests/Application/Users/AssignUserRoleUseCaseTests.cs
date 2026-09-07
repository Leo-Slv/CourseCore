using CourseCore.Api.Modules.Users.Application.UseCases;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Users;

public class AssignUserRoleUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUserAndRoleExist_ShouldAssignRole()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var user = TestEntityFactory.User();
        var role = TestEntityFactory.Role();
        users.Add(user);
        await roles.CreateAsync(role);
        var auditLogs = new FakeAuditLogService();
        var useCase = new AssignUserRoleUseCase(users, roles, new FakeUnitOfWork(), auditLogs);

        await useCase.ExecuteAsync(user.Id, role.Id);

        var assignedRoles = await roles.FindByUserIdAsync(user.Id);
        Assert.Contains(assignedRoles, r => r.Id == role.Id);
        Assert.Contains(auditLogs.Entries, entry => entry.Action == "UserRoleAssigned");
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        var roles = new FakeRoleRepository();
        var role = TestEntityFactory.Role();
        await roles.CreateAsync(role);
        var useCase = new AssignUserRoleUseCase(new FakeUserRepository(), roles, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid(), role.Id));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleDoesNotExist_ShouldThrowNotFoundException()
    {
        var users = new FakeUserRepository();
        var user = TestEntityFactory.User();
        users.Add(user);
        var useCase = new AssignUserRoleUseCase(users, new FakeRoleRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(user.Id, Guid.NewGuid()));
    }
}
