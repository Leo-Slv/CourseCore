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

    [Fact]
    public void RegisterWatch_WhenPositionIsGiven_ShouldRememberWhereThePlayerStopped()
    {
        var progress = UserLessonProgress.Create(Guid.NewGuid(), Guid.NewGuid());

        progress.RegisterWatch(30, maxWatchedSeconds: 100, positionSeconds: 45);

        Assert.Equal(45, progress.LastPositionSeconds);
        Assert.Equal(30, progress.WatchedSeconds);
    }

    [Fact]
    public void RegisterWatch_WhenPositionGoesBackwards_ShouldKeepTheLatestPositionButNeverLowerWatchedSeconds()
    {
        var progress = UserLessonProgress.Create(Guid.NewGuid(), Guid.NewGuid());
        progress.RegisterWatch(60, maxWatchedSeconds: 100, positionSeconds: 60);

        progress.RegisterWatch(20, maxWatchedSeconds: 100, positionSeconds: 10);

        Assert.Equal(10, progress.LastPositionSeconds);
        Assert.Equal(60, progress.WatchedSeconds);
    }

    [Fact]
    public void RegisterWatch_WhenPositionExceedsDuration_ShouldClampToDuration()
    {
        var progress = UserLessonProgress.Create(Guid.NewGuid(), Guid.NewGuid());

        progress.RegisterWatch(10, maxWatchedSeconds: 100, positionSeconds: 5000);

        Assert.Equal(100, progress.LastPositionSeconds);
    }

    [Fact]
    public void RegisterWatch_WhenPositionIsOmitted_ShouldKeepThePreviousPosition()
    {
        var progress = UserLessonProgress.Create(Guid.NewGuid(), Guid.NewGuid());
        progress.RegisterWatch(10, maxWatchedSeconds: 100, positionSeconds: 8);

        progress.RegisterWatch(20, maxWatchedSeconds: 100);

        Assert.Equal(8, progress.LastPositionSeconds);
    }
}
