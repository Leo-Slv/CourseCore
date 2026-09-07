using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Auth.Infrastructure.Persistence.Mappers;

public static class PasswordResetTokenMapper
{
    public static PasswordResetToken ToDomain(PasswordResetTokenPersistenceModel model)
    {
        return PasswordResetToken.Restore(
            model.Id,
            model.UserId,
            model.TokenHash,
            model.ExpiresAt,
            model.CreatedAt,
            model.ConsumedAt);
    }

    public static PasswordResetTokenPersistenceModel ToPersistence(PasswordResetToken token)
    {
        return new PasswordResetTokenPersistenceModel
        {
            Id = token.Id,
            UserId = token.UserId,
            TokenHash = token.TokenHash,
            ExpiresAt = token.ExpiresAt,
            CreatedAt = token.CreatedAt,
            ConsumedAt = token.ConsumedAt
        };
    }
}
