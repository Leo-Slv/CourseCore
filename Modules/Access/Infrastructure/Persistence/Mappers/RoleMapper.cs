using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Mappers;

public static class RoleMapper
{
    public static Role ToDomain(RolePersistenceModel model)
    {
        return Role.Restore(
            model.Id,
            model.Name,
            model.Description,
            model.Active,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static RolePersistenceModel ToPersistence(Role role)
    {
        return new RolePersistenceModel
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Active = role.Active,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }

    public static void ApplyChanges(Role role, RolePersistenceModel model)
    {
        model.Name = role.Name;
        model.Description = role.Description;
        model.Active = role.Active;
        model.UpdatedAt = role.UpdatedAt;
    }
}
