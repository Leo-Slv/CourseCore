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
