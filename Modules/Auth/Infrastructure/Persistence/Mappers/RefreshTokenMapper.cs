using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Auth.Infrastructure.Persistence.Mappers;

public static class RefreshTokenMapper
{
    public static RefreshToken ToDomain(RefreshTokenPersistenceModel model)
    {
        return RefreshToken.Restore(
            model.Id,
            model.UserId,
            model.TokenHash,
            model.ExpiresAt,
            model.CreatedAt,
            model.RevokedAt,
            model.ReplacedByTokenHash);
    }

    public static RefreshTokenPersistenceModel ToPersistence(RefreshToken refreshToken)
    {
        return new RefreshTokenPersistenceModel
        {
            Id = refreshToken.Id,
            UserId = refreshToken.UserId,
            TokenHash = refreshToken.TokenHash,
            ExpiresAt = refreshToken.ExpiresAt,
            CreatedAt = refreshToken.CreatedAt,
            RevokedAt = refreshToken.RevokedAt,
            ReplacedByTokenHash = refreshToken.ReplacedByTokenHash
        };
    }

    public static void ApplyChanges(
        RefreshToken refreshToken,
        RefreshTokenPersistenceModel model)
    {
        model.UserId = refreshToken.UserId;
        model.TokenHash = refreshToken.TokenHash;
        model.ExpiresAt = refreshToken.ExpiresAt;
        model.CreatedAt = refreshToken.CreatedAt;
        model.RevokedAt = refreshToken.RevokedAt;
        model.ReplacedByTokenHash = refreshToken.ReplacedByTokenHash;
    }
}
