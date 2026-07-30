using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Users.Application.DTOs;
using CourseCore.Api.Modules.Users.Application.UseCases;
using CourseCore.Api.Tests.TestDoubles;

namespace CourseCore.Api.Tests.Application.Users;

public class UpdateUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUserIsDeactivated_ShouldIncrementTokenVersionAndRevokeActiveRefreshTokens()
    {
        var user = TestEntityFactory.User(tokenVersion: 0, active: true);
        var users = new FakeUserRepository();
        var refreshTokens = new FakeRefreshTokenRepository();
        users.Add(user);
        refreshTokens.AddExisting(RefreshToken.Create(
            user.Id,
            "hash:refresh-token",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddMinutes(-10)));
        var useCase = new UpdateUserUseCase(
            users,
            refreshTokens,
            new FakeUnitOfWork(),
            new FakeAuditLogService());

        await useCase.ExecuteAsync(new UpdateUserInput
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email.Value,
            Active = false
        });

        Assert.False(user.Active);
        Assert.Equal(1, user.TokenVersion);
        Assert.Equal(1, refreshTokens.RevokeActiveByUserIdCalls);
        Assert.Equal(user.Id, refreshTokens.LastRevokedUserId);
    }
}
