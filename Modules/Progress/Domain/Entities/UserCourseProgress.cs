using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Progress.Domain.Entities;

public class UserCourseProgress : EntityBase
{
    private UserCourseProgress(
        Guid userId,
        Guid courseId,
        decimal progressPercent,
        DateTime startedAt,
        DateTime? completedAt)
    {
        UserId = ValidateId(userId, nameof(UserId));
        CourseId = ValidateId(courseId, nameof(CourseId));
        ProgressPercent = ValidateProgressPercent(progressPercent);
        StartedAt = startedAt;
        CompletedAt = completedAt;
    }

    public Guid UserId { get; private set; }

    public Guid CourseId { get; private set; }

    public decimal ProgressPercent { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public static UserCourseProgress Create(Guid userId, Guid courseId)
    {
        return new UserCourseProgress(userId, courseId, progressPercent: 0, DateTime.UtcNow, completedAt: null);
    }

    public static UserCourseProgress Restore(
        Guid id,
        Guid userId,
        Guid courseId,
        decimal progressPercent,
        DateTime startedAt,
        DateTime? completedAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new UserCourseProgress(userId, courseId, progressPercent, startedAt, completedAt)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void Recalculate(decimal progressPercent)
    {
        ProgressPercent = ValidateProgressPercent(progressPercent);

        if (ProgressPercent < 100)
        {
            CompletedAt = null;
        }

        MarkAsUpdated();
    }

    public void MarkAsCompleted()
    {
        ProgressPercent = 100;
        CompletedAt = DateTime.UtcNow;
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

    private static decimal ValidateProgressPercent(decimal progressPercent)
    {
        if (progressPercent < 0 || progressPercent > 100)
        {
            throw new DomainException("ProgressPercent must be between 0 and 100.");
        }

        return progressPercent;
    }
}
