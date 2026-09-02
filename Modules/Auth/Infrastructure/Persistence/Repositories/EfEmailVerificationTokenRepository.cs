using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Auth.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Auth.Infrastructure.Persistence.Repositories;

public class EfEmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfEmailVerificationTokenRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmailVerificationToken?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.EmailVerificationTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        return model is null ? null : EmailVerificationTokenMapper.ToDomain(model);
    }

    public async Task AddAsync(
        EmailVerificationToken token,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.EmailVerificationTokens.AddAsync(
            EmailVerificationTokenMapper.ToPersistence(token),
            cancellationToken);
    }

    public async Task<bool> TryConsumeAsync(
        Guid tokenId,
        string currentTokenHash,
        DateTime consumedAt,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _dbContext.EmailVerificationTokens
            .Where(x => x.Id == tokenId
                && x.TokenHash == currentTokenHash
                && x.ConsumedAt == null
                && x.ExpiresAt > consumedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.ConsumedAt, consumedAt),
                cancellationToken);

        return affectedRows == 1;
    }

    public Task InvalidateActiveByUserIdAsync(
        Guid userId,
        DateTime consumedAt,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EmailVerificationTokens
            .Where(x => x.UserId == userId
                && x.ConsumedAt == null
                && x.ExpiresAt > consumedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.ConsumedAt, consumedAt),
                cancellationToken);
    }
}
