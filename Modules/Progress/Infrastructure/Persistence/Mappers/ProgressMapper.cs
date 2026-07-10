using CourseCore.Api.Modules.Progress.Domain.Entities;
using CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Progress.Infrastructure.Persistence.Mappers;

public static class ProgressMapper
{
    public static UserCourseProgress ToDomain(UserCourseProgressPersistenceModel model)
    {
        return UserCourseProgress.Restore(
            model.Id,
            model.UserId,
            model.CourseId,
            model.ProgressPercent,
            model.StartedAt,
            model.CompletedAt,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static UserCourseProgressPersistenceModel ToPersistence(UserCourseProgress progress)
    {
        return new UserCourseProgressPersistenceModel
        {
            Id = progress.Id,
            UserId = progress.UserId,
            CourseId = progress.CourseId,
            ProgressPercent = progress.ProgressPercent,
            StartedAt = progress.StartedAt,
            CompletedAt = progress.CompletedAt,
            CreatedAt = progress.CreatedAt,
            UpdatedAt = progress.UpdatedAt
        };
    }

    public static void ApplyChanges(UserCourseProgress progress, UserCourseProgressPersistenceModel model)
    {
        model.UserId = progress.UserId;
        model.CourseId = progress.CourseId;
        model.ProgressPercent = progress.ProgressPercent;
        model.StartedAt = progress.StartedAt;
        model.CompletedAt = progress.CompletedAt;
        model.UpdatedAt = progress.UpdatedAt;
    }

    public static UserLessonProgress ToDomain(UserLessonProgressPersistenceModel model)
    {
        return UserLessonProgress.Restore(
            model.Id,
            model.UserId,
            model.LessonId,
            model.Completed,
            model.WatchedSeconds,
            model.LastWatchedAt,
            model.CompletedAt,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static UserLessonProgressPersistenceModel ToPersistence(UserLessonProgress progress)
    {
        return new UserLessonProgressPersistenceModel
        {
            Id = progress.Id,
            UserId = progress.UserId,
            LessonId = progress.LessonId,
            Completed = progress.Completed,
            WatchedSeconds = progress.WatchedSeconds,
            LastWatchedAt = progress.LastWatchedAt,
            CompletedAt = progress.CompletedAt,
            CreatedAt = progress.CreatedAt,
            UpdatedAt = progress.UpdatedAt
        };
    }

    public static void ApplyChanges(UserLessonProgress progress, UserLessonProgressPersistenceModel model)
    {
        model.UserId = progress.UserId;
        model.LessonId = progress.LessonId;
        model.Completed = progress.Completed;
        model.WatchedSeconds = progress.WatchedSeconds;
        model.LastWatchedAt = progress.LastWatchedAt;
        model.CompletedAt = progress.CompletedAt;
        model.UpdatedAt = progress.UpdatedAt;
    }
}
