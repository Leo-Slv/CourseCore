using CourseCore.Api.Modules.Auth.Domain.Entities;

namespace CourseCore.Api.Modules.Auth.Domain.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default);

    Task<bool> TryConsumeAsync(
        Guid tokenId,
        string currentTokenHash,
        DateTime consumedAt,
        CancellationToken cancellationToken = default);

    Task InvalidateActiveByUserIdAsync(
        Guid userId,
        DateTime consumedAt,
        CancellationToken cancellationToken = default);
}
