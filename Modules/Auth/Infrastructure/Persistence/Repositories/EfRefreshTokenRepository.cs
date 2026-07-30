using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Domain.Repositories;
using CourseCore.Api.Modules.Auth.Infrastructure.Persistence.Mappers;
using CourseCore.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourseCore.Api.Modules.Auth.Infrastructure.Persistence.Repositories;

public class EfRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly CourseCoreDbContext _dbContext;

    public EfRefreshTokenRepository(CourseCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshToken?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        return model is null ? null : RefreshTokenMapper.ToDomain(model);
    }

    public async Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.RefreshTokens.AddAsync(
            RefreshTokenMapper.ToPersistence(refreshToken),
            cancellationToken);
    }

    public async Task<bool> TryRotateAsync(
        Guid refreshTokenId,
        string currentTokenHash,
        string replacementTokenHash,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _dbContext.RefreshTokens
            .Where(x => x.Id == refreshTokenId
                && x.TokenHash == currentTokenHash
                && x.RevokedAt == null
                && x.ExpiresAt > revokedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.RevokedAt, revokedAt)
                .SetProperty(x => x.ReplacedByTokenHash, replacementTokenHash),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryRevokeAsync(
        Guid refreshTokenId,
        string currentTokenHash,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _dbContext.RefreshTokens
            .Where(x => x.Id == refreshTokenId
                && x.TokenHash == currentTokenHash
                && x.RevokedAt == null
                && x.ExpiresAt > revokedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.RevokedAt, revokedAt),
                cancellationToken);

        return affectedRows == 1;
    }

    public Task<int> RevokeActiveByUserIdAsync(
        Guid userId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens
            .Where(x => x.UserId == userId
                && x.RevokedAt == null
                && x.ExpiresAt > revokedAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.RevokedAt, revokedAt),
                cancellationToken);
    }

    public async Task UpdateAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        var model = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Id == refreshToken.Id, cancellationToken);

        if (model is null)
        {
            throw new InvalidOperationException("Refresh token not found.");
        }

        RefreshTokenMapper.ApplyChanges(refreshToken, model);
    }
}
