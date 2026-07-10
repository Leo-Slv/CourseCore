using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Access.Domain.Entities;

public class UserAreaAccess : EntityBase
{
    private UserAreaAccess(
        Guid userId,
        Guid areaId,
        bool canView,
        bool canManage,
        DateTime? startsAt,
        DateTime? expiresAt)
    {
        UserId = ValidateId(userId, nameof(UserId));
        AreaId = ValidateId(areaId, nameof(AreaId));
        CanView = canView;
        CanManage = canManage;
        ValidatePeriod(startsAt, expiresAt);
        StartsAt = startsAt;
        ExpiresAt = expiresAt;
    }

    public Guid UserId { get; private set; }

    public Guid AreaId { get; private set; }

    public bool CanView { get; private set; }

    public bool CanManage { get; private set; }

    public DateTime? StartsAt { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public static UserAreaAccess Create(
        Guid userId,
        Guid areaId,
        bool canView,
        bool canManage,
        DateTime? startsAt = null,
        DateTime? expiresAt = null)
    {
        return new UserAreaAccess(userId, areaId, canView, canManage, startsAt, expiresAt);
    }

    public static UserAreaAccess Restore(
        Guid id,
        Guid userId,
        Guid areaId,
        bool canView,
        bool canManage,
        DateTime? startsAt,
        DateTime? expiresAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new UserAreaAccess(userId, areaId, canView, canManage, startsAt, expiresAt)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public bool IsValidAt(DateTime date)
    {
        if (!CanView && !CanManage)
        {
            return false;
        }

        if (StartsAt.HasValue && date < StartsAt.Value)
        {
            return false;
        }

        if (ExpiresAt.HasValue && date > ExpiresAt.Value)
        {
            return false;
        }

        return true;
    }

    public void ChangePermissions(bool canView, bool canManage)
    {
        CanView = canView;
        CanManage = canManage;
        MarkAsUpdated();
    }

    public void ChangePeriod(DateTime? startsAt, DateTime? expiresAt)
    {
        ValidatePeriod(startsAt, expiresAt);
        StartsAt = startsAt;
        ExpiresAt = expiresAt;
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

    private static void ValidatePeriod(DateTime? startsAt, DateTime? expiresAt)
    {
        if (startsAt.HasValue && expiresAt.HasValue && expiresAt.Value < startsAt.Value)
        {
            throw new DomainException("ExpiresAt cannot be earlier than StartsAt.");
        }
    }
}
