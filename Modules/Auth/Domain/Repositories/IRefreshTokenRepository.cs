using CourseCore.Api.Modules.Auth.Domain.Entities;

namespace CourseCore.Api.Modules.Auth.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);
}
