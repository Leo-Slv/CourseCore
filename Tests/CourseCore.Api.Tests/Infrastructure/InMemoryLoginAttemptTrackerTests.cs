using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Infrastructure;

public class InMemoryLoginAttemptTrackerTests
{
    private static (InMemoryLoginAttemptTracker Tracker, ManualTimeProvider Time) Create(
        int maxFailures = 3,
        int windowMinutes = 10,
        int lockoutMinutes = 15)
    {
        var time = new ManualTimeProvider();
        var options = Options.Create(new LoginLockoutOptions
        {
            MaxFailedAttempts = maxFailures,
            FailureWindowMinutes = windowMinutes,
            LockoutMinutes = lockoutMinutes
        });

        return (new InMemoryLoginAttemptTracker(options, time), time);
    }

    [Fact]
    public void BelowTheLimit_ShouldNotLock()
    {
        var (tracker, _) = Create();

        tracker.RegisterFailure("a@x.com");
        tracker.RegisterFailure("a@x.com");

        Assert.Null(tracker.GetRemainingLockout("a@x.com"));
    }

    [Fact]
    public void ReachingTheLimit_ShouldLockForTheConfiguredTime()
    {
        var (tracker, _) = Create(lockoutMinutes: 15);

        for (var i = 0; i < 3; i++)
        {
            tracker.RegisterFailure("a@x.com");
        }

        Assert.Equal(TimeSpan.FromMinutes(15), tracker.GetRemainingLockout("a@x.com"));
    }

    [Fact]
    public void TheLockout_ShouldCountDownAndThenEnd()
    {
        var (tracker, time) = Create(lockoutMinutes: 15);

        for (var i = 0; i < 3; i++)
        {
            tracker.RegisterFailure("a@x.com");
        }

        time.Advance(TimeSpan.FromMinutes(10));
        Assert.Equal(TimeSpan.FromMinutes(5), tracker.GetRemainingLockout("a@x.com"));

        time.Advance(TimeSpan.FromMinutes(6));
        Assert.Null(tracker.GetRemainingLockout("a@x.com"));
    }

    [Fact]
    public void FailuresOutsideTheWindow_ShouldBeForgotten()
    {
        var (tracker, time) = Create(windowMinutes: 10);
        tracker.RegisterFailure("a@x.com");
        tracker.RegisterFailure("a@x.com");

        time.Advance(TimeSpan.FromMinutes(11));
        tracker.RegisterFailure("a@x.com");

        Assert.Null(tracker.GetRemainingLockout("a@x.com"));
    }

    [Fact]
    public void ASuccess_ShouldClearThePreviousFailures()
    {
        var (tracker, _) = Create();
        tracker.RegisterFailure("a@x.com");
        tracker.RegisterFailure("a@x.com");

        tracker.RegisterSuccess("a@x.com");
        tracker.RegisterFailure("a@x.com");
        tracker.RegisterFailure("a@x.com");

        Assert.Null(tracker.GetRemainingLockout("a@x.com"));
    }

    [Fact]
    public void TheEmail_ShouldBeComparedIgnoringCaseAndSpaces()
    {
        var (tracker, _) = Create();

        tracker.RegisterFailure("User@X.com");
        tracker.RegisterFailure(" user@x.com ");
        tracker.RegisterFailure("USER@x.COM");

        Assert.NotNull(tracker.GetRemainingLockout("user@x.com"));
    }

    [Fact]
    public void AFailureWhileLocked_ShouldNotExtendTheLockout()
    {
        var (tracker, time) = Create(lockoutMinutes: 15);

        for (var i = 0; i < 3; i++)
        {
            tracker.RegisterFailure("a@x.com");
        }

        time.Advance(TimeSpan.FromMinutes(10));
        tracker.RegisterFailure("a@x.com");

        Assert.Equal(TimeSpan.FromMinutes(5), tracker.GetRemainingLockout("a@x.com"));
    }
}
