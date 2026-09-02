using CourseCore.Api.Modules.Auth.Domain.Entities;
using CourseCore.Api.Modules.Auth.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Auth.Infrastructure.Persistence.Mappers;

public static class EmailVerificationTokenMapper
{
    public static EmailVerificationToken ToDomain(EmailVerificationTokenPersistenceModel model)
    {
        return EmailVerificationToken.Restore(
            model.Id,
            model.UserId,
            model.TokenHash,
            model.ExpiresAt,
            model.CreatedAt,
            model.ConsumedAt);
    }

    public static EmailVerificationTokenPersistenceModel ToPersistence(EmailVerificationToken token)
    {
        return new EmailVerificationTokenPersistenceModel
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
