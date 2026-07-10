using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Progress.Domain.Entities;

public class UserLessonProgress : EntityBase
{
    private UserLessonProgress(
        Guid userId,
        Guid lessonId,
        bool completed,
        int watchedSeconds,
        DateTime lastWatchedAt,
        DateTime? completedAt)
    {
        UserId = ValidateId(userId, nameof(UserId));
        LessonId = ValidateId(lessonId, nameof(LessonId));
        Completed = completed;
        WatchedSeconds = ValidateWatchedSeconds(watchedSeconds);
        LastWatchedAt = lastWatchedAt;
        CompletedAt = completedAt;
    }

    public Guid UserId { get; private set; }

    public Guid LessonId { get; private set; }

    public bool Completed { get; private set; }

    public int WatchedSeconds { get; private set; }

    public DateTime LastWatchedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public static UserLessonProgress Create(Guid userId, Guid lessonId)
    {
        var now = DateTime.UtcNow;
        return new UserLessonProgress(userId, lessonId, completed: false, watchedSeconds: 0, now, completedAt: null);
    }

    public static UserLessonProgress Restore(
        Guid id,
        Guid userId,
        Guid lessonId,
        bool completed,
        int watchedSeconds,
        DateTime lastWatchedAt,
        DateTime? completedAt,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new UserLessonProgress(userId, lessonId, completed, watchedSeconds, lastWatchedAt, completedAt)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void RegisterWatch(int watchedSeconds)
    {
        var validatedWatchedSeconds = ValidateWatchedSeconds(watchedSeconds);

        if (validatedWatchedSeconds > WatchedSeconds)
        {
            WatchedSeconds = validatedWatchedSeconds;
        }

        LastWatchedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void MarkAsCompleted()
    {
        Completed = true;
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

    private static int ValidateWatchedSeconds(int watchedSeconds)
    {
        if (watchedSeconds < 0)
        {
            throw new DomainException("WatchedSeconds cannot be negative.");
        }

        return watchedSeconds;
    }
}
