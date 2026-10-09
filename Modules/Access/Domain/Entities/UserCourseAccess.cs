using CourseCore.Api.Modules.Access.Domain.Enums;
using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Access.Domain.Entities;

public class UserCourseAccess : EntityBase
{
    private UserCourseAccess(Guid userId, Guid courseId, CourseAccessSource source)
    {
        UserId = ValidateId(userId, nameof(UserId));
        CourseId = ValidateId(courseId, nameof(CourseId));
        Source = source;
    }

    public Guid UserId { get; private set; }

    public Guid CourseId { get; private set; }

    public CourseAccessSource Source { get; private set; }

    public static UserCourseAccess Create(Guid userId, Guid courseId, CourseAccessSource source)
    {
        return new UserCourseAccess(userId, courseId, source);
    }

    public static UserCourseAccess Restore(
        Guid id,
        Guid userId,
        Guid courseId,
        CourseAccessSource source,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new UserCourseAccess(userId, courseId, source)
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
