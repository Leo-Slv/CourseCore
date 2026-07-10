using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Mappers;

public static class PermissionMapper
{
    public static Permission ToDomain(PermissionPersistenceModel model)
    {
        return Permission.Restore(
            model.Id,
            model.Key,
            model.Name,
            model.Description,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static PermissionPersistenceModel ToPersistence(Permission permission)
    {
        return new PermissionPersistenceModel
        {
            Id = permission.Id,
            Key = permission.Key,
            Name = permission.Name,
            Description = permission.Description,
            CreatedAt = permission.CreatedAt,
            UpdatedAt = permission.UpdatedAt
        };
    }

    public static void ApplyChanges(Permission permission, PermissionPersistenceModel model)
    {
        model.Key = permission.Key;
        model.Name = permission.Name;
        model.Description = permission.Description;
        model.UpdatedAt = permission.UpdatedAt;
    }
}
