using CourseCore.Api.Modules.Auth.Domain.Entities;

namespace CourseCore.Api.Modules.Auth.Domain.Repositories;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        EmailVerificationToken token,
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
