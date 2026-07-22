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
    public void MarkAsCompleted_WhenCalled_ShouldCompleteLesson()
    {
        var progress = UserLessonProgress.Create(Guid.NewGuid(), Guid.NewGuid());

        progress.MarkAsCompleted();

        Assert.True(progress.Completed);
        Assert.NotNull(progress.CompletedAt);
    }
}
