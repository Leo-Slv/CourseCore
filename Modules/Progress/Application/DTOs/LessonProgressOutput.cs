using CourseCore.Api.Modules.Progress.Domain.Entities;

namespace CourseCore.Api.Modules.Progress.Application.DTOs;

public class LessonProgressOutput
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public Guid LessonId { get; init; }

    public bool Completed { get; init; }

    public int WatchedSeconds { get; init; }

    public DateTime LastWatchedAt { get; init; }

    public DateTime? CompletedAt { get; init; }

    public static LessonProgressOutput FromProgress(UserLessonProgress progress)
    {
        return new LessonProgressOutput
        {
            Id = progress.Id,
            UserId = progress.UserId,
            LessonId = progress.LessonId,
            Completed = progress.Completed,
            WatchedSeconds = progress.WatchedSeconds,
            LastWatchedAt = progress.LastWatchedAt,
            CompletedAt = progress.CompletedAt
        };
    }
}
