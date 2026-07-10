using CourseCore.Api.Modules.Users.Domain.Entities;
using CourseCore.Api.Modules.Users.Infrastructure.Persistence.Models;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Users.Infrastructure.Persistence.Mappers;

public static class UserMapper
{
    public static User ToDomain(UserPersistenceModel model)
    {
        return User.Restore(
            model.Id,
            model.Name,
            Email.Create(model.Email),
            model.PasswordHash,
            model.Active,
            model.EmailVerifiedAt,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static UserPersistenceModel ToPersistence(User user)
    {
        return new UserPersistenceModel
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email.Value,
            PasswordHash = user.PasswordHash,
            Active = user.Active,
            EmailVerifiedAt = user.EmailVerifiedAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public static void ApplyChanges(User user, UserPersistenceModel model)
    {
        model.Name = user.Name;
        model.Email = user.Email.Value;
        model.PasswordHash = user.PasswordHash;
        model.Active = user.Active;
        model.EmailVerifiedAt = user.EmailVerifiedAt;
        model.UpdatedAt = user.UpdatedAt;
    }
}
