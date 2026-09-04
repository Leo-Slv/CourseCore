using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Access.Domain.Entities;

public class AccessRequest : EntityBase
{
    private AccessRequest(
        Guid userId,
        Guid courseId,
        AccessRequestStatus status,
        DateTime? decidedAt,
        Guid? decidedByUserId)
    {
        UserId = ValidateId(userId, nameof(UserId));
        CourseId = ValidateId(courseId, nameof(CourseId));
        Status = status;
        DecidedAt = decidedAt;
        DecidedByUserId = decidedByUserId;
    }

    public Guid UserId { get; private set; }

    public Guid CourseId { get; private set; }

    public AccessRequestStatus Status { get; private set; }

    public DateTime? DecidedAt { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public static AccessRequest Create(Guid userId, Guid courseId)
    {
        return new AccessRequest(userId, courseId, AccessRequestStatus.Pending, decidedAt: null, decidedByUserId: null);
    }

    public static AccessRequest Restore(
        Guid id,
        Guid userId,
        Guid courseId,
        AccessRequestStatus status,
        DateTime? decidedAt,
        Guid? decidedByUserId,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new AccessRequest(userId, courseId, status, decidedAt, decidedByUserId)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void Approve(Guid decidedByUserId)
    {
        EnsurePending();
        Status = AccessRequestStatus.Approved;
        DecidedAt = DateTime.UtcNow;
        DecidedByUserId = ValidateId(decidedByUserId, nameof(DecidedByUserId));
        MarkAsUpdated();
    }

    public void Reject(Guid decidedByUserId)
    {
        EnsurePending();
        Status = AccessRequestStatus.Rejected;
        DecidedAt = DateTime.UtcNow;
        DecidedByUserId = ValidateId(decidedByUserId, nameof(DecidedByUserId));
        MarkAsUpdated();
    }

    private void EnsurePending()
    {
        if (Status != AccessRequestStatus.Pending)
        {
            throw new DomainException("Access request has already been decided.");
        }
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
