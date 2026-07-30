using CourseCore.Api.Modules.Progress.Domain.Entities;

namespace CourseCore.Api.Tests.Domain.Progress;

public class UserLessonProgressTests
{
    [Fact]
    public void RegisterWatch_WhenSecondsIncrease_ShouldUpdateWatchedSeconds()
    {
        var progress = UserLessonProgress.Create(Guid.NewGuid(), Guid.NewGuid());

        progress.RegisterWatch(120);

        Assert.Equal(120, progress.WatchedSeconds);
    }

    [Fact]
    public void RegisterWatch_WhenSecondsDecrease_ShouldKeepHighestWatchedSeconds()
    {
        var progress = UserLessonProgress.Create(Guid.NewGuid(), Guid.NewGuid());
        progress.RegisterWatch(120);

        progress.RegisterWatch(60);

        Assert.Equal(120, progress.WatchedSeconds);
    }

    [Fact]
    public void RegisterWatch_WhenSecondsExceedMaximum_ShouldClampWatchedSeconds()
    {
        var progress = UserLessonProgress.Create(Guid.NewGuid(), Guid.NewGuid());

        progress.RegisterWatch(150, maxWatchedSeconds: 120);

        Assert.Equal(120, progress.WatchedSeconds);
    }

    [Fact]
    public void RegisterWatch_WhenExistingProgressExceedsMaximum_ShouldClampWatchedSeconds()
    {
        var progress = UserLessonProgress.Create(Guid.NewGuid(), Guid.NewGuid());
        progress.RegisterWatch(300);

        progress.RegisterWatch(10, maxWatchedSeconds: 120);

        Assert.Equal(120, progress.WatchedSeconds);
    }

    [Fact]
    public void RecalculateCompletion_WhenCompletedIsTrue_ShouldCompleteLesson()
    {
        var progress = UserLessonProgress.Create(Guid.NewGuid(), Guid.NewGuid());

        progress.RecalculateCompletion(completed: true);

        Assert.True(progress.Completed);
        Assert.NotNull(progress.CompletedAt);
    }

    [Fact]
    public void RecalculateCompletion_WhenCompletedIsFalse_ShouldClearCompletion()
    {
        var progress = UserLessonProgress.Create(Guid.NewGuid(), Guid.NewGuid());
        progress.RecalculateCompletion(completed: true);

        progress.RecalculateCompletion(completed: false);

        Assert.False(progress.Completed);
        Assert.Null(progress.CompletedAt);
    }
}
