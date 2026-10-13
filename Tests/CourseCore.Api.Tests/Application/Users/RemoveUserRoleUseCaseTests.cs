using CourseCore.Api.Modules.Users.Application.Services;
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
        var role = TestEntityFactory.Role(name: "Secretaria");
        users.Add(user);
        await roles.CreateAsync(role);
        roles.AddForUser(user.Id, role);
        var auditLogs = new FakeAuditLogService();
        var useCase = new RemoveUserRoleUseCase(users, roles, new FakeUnitOfWork(), auditLogs, new UserSessionInvalidator(users, new FakeRefreshTokenRepository()));

        await useCase.ExecuteAsync(user.Id, role.Id);

        var remainingRoles = await roles.FindByUserIdAsync(user.Id);
        Assert.DoesNotContain(remainingRoles, r => r.Id == role.Id);
        var auditLog = Assert.Single(auditLogs.Entries, entry => entry.Action == "UserRoleUnassigned");
        Assert.Equal($"{user.Email.Value} → {role.Name}", auditLog.Metadata["displayName"]);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        var roles = new FakeRoleRepository();
        var role = TestEntityFactory.Role();
        await roles.CreateAsync(role);
        var users = new FakeUserRepository();
        var useCase = new RemoveUserRoleUseCase(users, roles, new FakeUnitOfWork(), new FakeAuditLogService(), new UserSessionInvalidator(users, new FakeRefreshTokenRepository()));

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid(), role.Id));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRemovingTheLastActiveAdmin_ShouldThrowConflictException()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var user = TestEntityFactory.User();
        var admin = TestEntityFactory.Role(name: "Admin");
        users.Add(user);
        await roles.CreateAsync(admin);
        roles.AddForUser(user.Id, admin);
        var useCase = new RemoveUserRoleUseCase(
            users, roles, new FakeUnitOfWork(), new FakeAuditLogService(),
            new UserSessionInvalidator(users, new FakeRefreshTokenRepository()));

        await Assert.ThrowsAsync<ConflictException>(() => useCase.ExecuteAsync(user.Id, admin.Id));

        Assert.Contains(await roles.FindByUserIdAsync(user.Id), r => r.Id == admin.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnotherAdminExists_ShouldRemoveAdminAndInvalidateSessions()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var refreshTokens = new FakeRefreshTokenRepository();
        var first = TestEntityFactory.User();
        var second = TestEntityFactory.User(email: "second.admin@coursecore.local");
        var admin = TestEntityFactory.Role(name: "Admin");
        users.Add(first);
        users.Add(second);
        await roles.CreateAsync(admin);
        roles.AddForUser(first.Id, admin);
        roles.AddForUser(second.Id, admin);
        var useCase = new RemoveUserRoleUseCase(
            users, roles, new FakeUnitOfWork(), new FakeAuditLogService(),
            new UserSessionInvalidator(users, refreshTokens));
        var versionBefore = second.TokenVersion;

        await useCase.ExecuteAsync(second.Id, admin.Id);

        Assert.DoesNotContain(await roles.FindByUserIdAsync(second.Id), r => r.Id == admin.Id);
        Assert.Equal(versionBefore + 1, (await users.FindByIdAsync(second.Id))!.TokenVersion);
        Assert.Equal(second.Id, refreshTokens.LastRevokedUserId);
    }
}
