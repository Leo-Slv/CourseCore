using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Certificates.Domain.Entities;

public class Certificate : EntityBase
{
    private Certificate(Guid userId, Guid courseId, DateTime issuedAt)
    {
        UserId = ValidateId(userId, nameof(UserId));
        CourseId = ValidateId(courseId, nameof(CourseId));
        IssuedAt = issuedAt;
    }

    public Guid UserId { get; private set; }

    public Guid CourseId { get; private set; }

    public DateTime IssuedAt { get; private set; }

    public static Certificate Issue(Guid userId, Guid courseId)
    {
        return new Certificate(userId, courseId, DateTime.UtcNow);
    }

    public static Certificate Restore(
        Guid id,
        Guid userId,
        Guid courseId,
        DateTime issuedAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new Certificate(userId, courseId, issuedAt)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
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
