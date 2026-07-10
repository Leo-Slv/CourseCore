using CourseCore.Api.Modules.Access.Domain.Entities;
using CourseCore.Api.Modules.Access.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Access.Infrastructure.Persistence.Mappers;

public static class UserAreaAccessMapper
{
    public static UserAreaAccess ToDomain(UserAreaAccessPersistenceModel model)
    {
        return UserAreaAccess.Restore(
            model.Id,
            model.UserId,
            model.AreaId,
            model.CanView,
            model.CanManage,
            model.StartsAt,
            model.ExpiresAt,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static UserAreaAccessPersistenceModel ToPersistence(UserAreaAccess access)
    {
        return new UserAreaAccessPersistenceModel
        {
            Id = access.Id,
            UserId = access.UserId,
            AreaId = access.AreaId,
            CanView = access.CanView,
            CanManage = access.CanManage,
            StartsAt = access.StartsAt,
            ExpiresAt = access.ExpiresAt,
            CreatedAt = access.CreatedAt,
            UpdatedAt = access.UpdatedAt
        };
    }

    public static void ApplyChanges(UserAreaAccess access, UserAreaAccessPersistenceModel model)
    {
        model.UserId = access.UserId;
        model.AreaId = access.AreaId;
        model.CanView = access.CanView;
        model.CanManage = access.CanManage;
        model.StartsAt = access.StartsAt;
        model.ExpiresAt = access.ExpiresAt;
        model.UpdatedAt = access.UpdatedAt;
    }
}
