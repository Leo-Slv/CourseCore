using CourseCore.Api.Modules.Progress.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Tests.Domain.Progress;

public class UserCourseProgressTests
{
    [Fact]
    public void Recalculate_WhenPercentIsInsideLimits_ShouldUpdateProgress()
    {
        var progress = UserCourseProgress.Create(Guid.NewGuid(), Guid.NewGuid());

        progress.Recalculate(75);

        Assert.Equal(75, progress.ProgressPercent);
    }

    [Fact]
    public void Recalculate_WhenPercentIsOutsideLimits_ShouldThrowDomainException()
    {
        var progress = UserCourseProgress.Create(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<DomainException>(() => progress.Recalculate(101));
    }

    [Fact]
    public void MarkAsCompleted_WhenCalled_ShouldSetProgressToOneHundred()
    {
        var progress = UserCourseProgress.Create(Guid.NewGuid(), Guid.NewGuid());

        progress.MarkAsCompleted();

        Assert.Equal(100, progress.ProgressPercent);
        Assert.NotNull(progress.CompletedAt);
    }
}
