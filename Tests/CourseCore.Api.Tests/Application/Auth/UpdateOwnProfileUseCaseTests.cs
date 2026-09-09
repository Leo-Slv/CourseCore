using CourseCore.Api.Modules.Auth.Application.DTOs;
using CourseCore.Api.Modules.Auth.Application.UseCases;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Shared.Application.Exceptions;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Auth;

public class UpdateOwnProfileUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldUpdateNamePhoneAndAvatarUrl()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User();
        fixture.Users.Add(user);

        var output = await fixture.UseCase.ExecuteAsync(new UpdateOwnProfileInput
        {
            UserId = user.Id,
            Name = "New Name",
            Phone = "+55 11 99999-0000",
            AvatarUrl = "https://cdn.coursecore.local/avatar.png"
        });

        Assert.Equal("New Name", output.Name);
        Assert.Equal("+55 11 99999-0000", output.Phone);
        Assert.Equal("https://cdn.coursecore.local/avatar.png", output.AvatarUrl);
        Assert.Contains(fixture.AuditLogs.Entries, entry => entry.Action == AuditLogActionNames.UserProfileUpdated);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNothingChanged_ShouldNotRecordAuditLog()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User();
        fixture.Users.Add(user);

        await fixture.UseCase.ExecuteAsync(new UpdateOwnProfileInput
        {
            UserId = user.Id,
            Name = user.Name,
            Phone = null,
            AvatarUrl = null
        });

        Assert.DoesNotContain(fixture.AuditLogs.Entries, entry => entry.Action == AuditLogActionNames.UserProfileUpdated);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotIncrementTokenVersionOrRevokeSessions()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User();
        fixture.Users.Add(user);
        var originalTokenVersion = user.TokenVersion;

        await fixture.UseCase.ExecuteAsync(new UpdateOwnProfileInput
        {
            UserId = user.Id,
            Name = "New Name"
        });

        Assert.Equal(originalTokenVersion, user.TokenVersion);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldThrow()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.UseCase.ExecuteAsync(new UpdateOwnProfileInput
        {
            UserId = Guid.NewGuid(),
            Name = "New Name"
        }));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNameIsBlank_ShouldThrow()
    {
        var fixture = CreateFixture();
        var user = TestEntityFactory.User();
        fixture.Users.Add(user);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => fixture.UseCase.ExecuteAsync(new UpdateOwnProfileInput
        {
            UserId = user.Id,
            Name = "   "
        }));
    }

    private static UpdateOwnProfileFixture CreateFixture()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository();
        var auditLogs = new FakeAuditLogService();

        var useCase = new UpdateOwnProfileUseCase(
            users,
            roles,
            new FakeUnitOfWork(),
            auditLogs);

        return new UpdateOwnProfileFixture(useCase, users, roles, auditLogs);
    }

    private sealed record UpdateOwnProfileFixture(
        UpdateOwnProfileUseCase UseCase,
        FakeUserRepository Users,
        FakeRoleRepository Roles,
        FakeAuditLogService AuditLogs);
}
