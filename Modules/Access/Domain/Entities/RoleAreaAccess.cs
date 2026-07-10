using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Access.Domain.Entities;

public class RoleAreaAccess : EntityBase
{
    private RoleAreaAccess(Guid roleId, Guid areaId, bool canView, bool canManage)
    {
        RoleId = ValidateId(roleId, nameof(RoleId));
        AreaId = ValidateId(areaId, nameof(AreaId));
        CanView = canView;
        CanManage = canManage;
    }

    public Guid RoleId { get; private set; }

    public Guid AreaId { get; private set; }

    public bool CanView { get; private set; }

    public bool CanManage { get; private set; }

    public static RoleAreaAccess Create(Guid roleId, Guid areaId, bool canView, bool canManage)
    {
        return new RoleAreaAccess(roleId, areaId, canView, canManage);
    }

    public static RoleAreaAccess Restore(
        Guid id,
        Guid roleId,
        Guid areaId,
        bool canView,
        bool canManage,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new RoleAreaAccess(roleId, areaId, canView, canManage)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void ChangePermissions(bool canView, bool canManage)
    {
        CanView = canView;
        CanManage = canManage;
        MarkAsUpdated();
    }

    public void Revoke()
    {
        CanView = false;
        CanManage = false;
        MarkAsUpdated();
    }

    private static Guid ValidateId(Guid id, string fieldName)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return id;
    }
}
