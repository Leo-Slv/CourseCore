using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Mappers;

public static class RoleAreaAccessMapper
{
    public static RoleAreaAccess ToDomain(RoleAreaAccessPersistenceModel model)
    {
        return RoleAreaAccess.Restore(
            model.Id,
            model.RoleId,
            model.AreaId,
            model.CanView,
            model.CanManage,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static RoleAreaAccessPersistenceModel ToPersistence(RoleAreaAccess access)
    {
        return new RoleAreaAccessPersistenceModel
        {
            Id = access.Id,
            RoleId = access.RoleId,
            AreaId = access.AreaId,
            CanView = access.CanView,
            CanManage = access.CanManage,
            CreatedAt = access.CreatedAt,
            UpdatedAt = access.UpdatedAt
        };
    }

    public static void ApplyChanges(RoleAreaAccess access, RoleAreaAccessPersistenceModel model)
    {
        model.RoleId = access.RoleId;
        model.AreaId = access.AreaId;
        model.CanView = access.CanView;
        model.CanManage = access.CanManage;
        model.UpdatedAt = access.UpdatedAt;
    }
}
