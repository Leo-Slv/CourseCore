using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Access.Application.UseCases;
using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.Users.Application.Services;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Access;

public class RoleManagementUseCaseTests
{
    [Fact]
    public async Task CreateRole_WhenNameIsFree_ShouldCreateAndAudit()
    {
        var roles = new FakeRoleRepository();
        var auditLogs = new FakeAuditLogService();
        var useCase = new CreateRoleUseCase(roles, new FakeUnitOfWork(), auditLogs);

        var output = await useCase.ExecuteAsync(new CreateRoleInput { Name = " Secretaria ", Description = "Equipe" });

        Assert.Equal("Secretaria", output.Name);
        Assert.Equal(0, output.MemberCount);
        Assert.NotNull(await roles.FindByNameAsync("Secretaria"));
        Assert.Single(auditLogs.Entries, entry => entry.Action == "RoleCreated");
    }

    [Fact]
    public async Task CreateRole_WhenNameAlreadyExists_ShouldThrowConflict()
    {
        var roles = new FakeRoleRepository();
        await roles.CreateAsync(TestEntityFactory.Role(name: "Secretaria"));
        var useCase = new CreateRoleUseCase(roles, new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ConflictException>(
            () => useCase.ExecuteAsync(new CreateRoleInput { Name = "Secretaria" }));
    }

    [Fact]
    public async Task CreateRole_WhenNameIsBlank_ShouldThrowValidation()
    {
        var useCase = new CreateRoleUseCase(new FakeRoleRepository(), new FakeUnitOfWork(), new FakeAuditLogService());

        await Assert.ThrowsAsync<ApplicationValidationException>(
            () => useCase.ExecuteAsync(new CreateRoleInput { Name = "   " }));
    }

    [Fact]
    public async Task UpdateRole_WhenTargetIsAdmin_ShouldThrowForbidden()
    {
        var roles = new FakeRoleRepository();
        var admin = TestEntityFactory.Role(name: "Admin");
        await roles.CreateAsync(admin);
        var useCase = CreateUpdate(roles, new FakeUserRepository(), new FakeRefreshTokenRepository());

        await Assert.ThrowsAsync<ForbiddenException>(() => useCase.ExecuteAsync(
            new UpdateRoleInput { RoleId = admin.Id, Name = "Admin", Active = false }));
    }

    [Fact]
    public async Task UpdateRole_WhenRenamingToAnExistingName_ShouldThrowConflict()
    {
        var roles = new FakeRoleRepository();
        var first = TestEntityFactory.Role(name: "Secretaria");
        var second = TestEntityFactory.Role(name: "Midia");
        await roles.CreateAsync(first);
        await roles.CreateAsync(second);
        var useCase = CreateUpdate(roles, new FakeUserRepository(), new FakeRefreshTokenRepository());

        await Assert.ThrowsAsync<ConflictException>(() => useCase.ExecuteAsync(
            new UpdateRoleInput { RoleId = second.Id, Name = "Secretaria", Active = true }));
    }

    [Fact]
    public async Task UpdateRole_WhenDeactivated_ShouldInvalidateMemberSessions()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var refreshTokens = new FakeRefreshTokenRepository();
        var member = TestEntityFactory.User();
        var role = TestEntityFactory.Role(name: "Secretaria");
        users.Add(member);
        await roles.CreateAsync(role);
        roles.AddForUser(member.Id, role);
        var useCase = CreateUpdate(roles, users, refreshTokens);

        var output = await useCase.ExecuteAsync(
            new UpdateRoleInput { RoleId = role.Id, Name = "Secretaria", Active = false });

        Assert.False(output.Active);
        Assert.Equal(1, (await users.FindByIdAsync(member.Id))!.TokenVersion);
        Assert.Equal(member.Id, refreshTokens.LastRevokedUserId);
    }

    [Fact]
    public async Task SetRolePermissions_ShouldReplacePermissionsAndInvalidateMembers()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var refreshTokens = new FakeRefreshTokenRepository();
        var member = TestEntityFactory.User();
        var role = TestEntityFactory.Role(name: "Secretaria");
        users.Add(member);
        await roles.CreateAsync(role);
        roles.AddForUser(member.Id, role);
        roles.PermissionKeysByRoleId[role.Id] = ["visitors.read", "courses.manage"];
        var auditLogs = new FakeAuditLogService();
        var useCase = CreateSetPermissions(roles, users, refreshTokens, auditLogs, "visitors.read", "visitors.manage", "courses.manage");

        var output = await useCase.ExecuteAsync(new SetRolePermissionsInput
        {
            RoleId = role.Id,
            PermissionKeys = ["visitors.read", "visitors.manage"]
        });

        Assert.Equal(["visitors.manage", "visitors.read"], output.PermissionKeys);
        Assert.Equal(["visitors.read", "visitors.manage"], roles.PermissionKeysByRoleId[role.Id]);
        Assert.Equal(1, (await users.FindByIdAsync(member.Id))!.TokenVersion);
        var audit = Assert.Single(auditLogs.Entries, entry => entry.Action == "RolePermissionsChanged");
        Assert.Equal("visitors.manage", audit.Metadata["granted"]);
        Assert.Equal("courses.manage", audit.Metadata["removed"]);
    }

    [Fact]
    public async Task SetRolePermissions_WhenKeyIsUnknown_ShouldThrowValidation()
    {
        var roles = new FakeRoleRepository();
        var role = TestEntityFactory.Role(name: "Secretaria");
        await roles.CreateAsync(role);
        var useCase = CreateSetPermissions(roles, new FakeUserRepository(), new FakeRefreshTokenRepository(), new FakeAuditLogService(), "visitors.read");

        await Assert.ThrowsAsync<ApplicationValidationException>(() => useCase.ExecuteAsync(
            new SetRolePermissionsInput { RoleId = role.Id, PermissionKeys = ["not.a.permission"] }));
    }

    [Fact]
    public async Task SetRolePermissions_WhenTargetIsAdmin_ShouldThrowForbidden()
    {
        var roles = new FakeRoleRepository();
        var admin = TestEntityFactory.Role(name: "Admin");
        await roles.CreateAsync(admin);
        var useCase = CreateSetPermissions(roles, new FakeUserRepository(), new FakeRefreshTokenRepository(), new FakeAuditLogService(), "visitors.read");

        await Assert.ThrowsAsync<ForbiddenException>(() => useCase.ExecuteAsync(
            new SetRolePermissionsInput { RoleId = admin.Id, PermissionKeys = [] }));
    }

    [Fact]
    public async Task ListRoleDetails_ShouldPutAdminFirstAndFlagItAsSystem()
    {
        var roles = new FakeRoleRepository();
        var secretaria = TestEntityFactory.Role(name: "Aaa Secretaria");
        var admin = TestEntityFactory.Role(name: "Admin");
        await roles.CreateAsync(secretaria);
        await roles.CreateAsync(admin);
        roles.PermissionKeysByRoleId[secretaria.Id] = ["visitors.read"];

        var output = await new ListRoleDetailsUseCase(roles).ExecuteAsync();

        Assert.Equal(["Admin", "Aaa Secretaria"], output.Select(role => role.Name).ToArray());
        Assert.True(output.First().IsSystem);
        Assert.Equal(["visitors.read"], output.Last().PermissionKeys);
    }

    private static UpdateRoleUseCase CreateUpdate(
        FakeRoleRepository roles,
        FakeUserRepository users,
        FakeRefreshTokenRepository refreshTokens)
    {
        return new UpdateRoleUseCase(
            roles,
            new UserSessionInvalidator(users, refreshTokens),
            new FakeUnitOfWork(),
            new FakeAuditLogService());
    }

    private static SetRolePermissionsUseCase CreateSetPermissions(
        FakeRoleRepository roles,
        FakeUserRepository users,
        FakeRefreshTokenRepository refreshTokens,
        FakeAuditLogService auditLogs,
        params string[] catalog)
    {
        return new SetRolePermissionsUseCase(
            roles,
            new StubPermissionRepository(catalog),
            new UserSessionInvalidator(users, refreshTokens),
            new FakeUnitOfWork(),
            auditLogs);
    }

    private sealed class StubPermissionRepository : IPermissionRepository
    {
        private readonly IReadOnlyCollection<Permission> _permissions;

        public StubPermissionRepository(IEnumerable<string> keys)
        {
            _permissions = keys.Select(key => Permission.Create(key, key, key)).ToList();
        }

        public Task<IReadOnlyCollection<Permission>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_permissions);
        }
    }
}
