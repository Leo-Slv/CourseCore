using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;

namespace CourseCore.Api.Tests.Domain.Media;

public class VideoVisibilityTests
{
    [Fact]
    public void Create_ShouldDefaultToActiveVisibility()
    {
        var video = Video.Create(
            Guid.NewGuid(), "Video", "Description", VideoStorageProvider.Local, "videos/video.mp4", 120, 1024);

        Assert.Equal(VideoVisibility.Active, video.Visibility);
    }

    [Fact]
    public void MarkAsUnlisted_ShouldSetVisibilityAndUpdateTimestamp()
    {
        var video = CreateVideo();
        var previousUpdatedAt = video.UpdatedAt;

        video.MarkAsUnlisted();

        Assert.Equal(VideoVisibility.Unlisted, video.Visibility);
        Assert.True(video.UpdatedAt >= previousUpdatedAt);
    }

    [Fact]
    public void MarkAsActive_AfterUnlisted_ShouldRestoreActiveVisibility()
    {
        var video = CreateVideo();
        video.MarkAsUnlisted();

        video.MarkAsActive();

        Assert.Equal(VideoVisibility.Active, video.Visibility);
    }

    private static Video CreateVideo()
    {
        return Video.Create(
            Guid.NewGuid(), "Video", "Description", VideoStorageProvider.Local, "videos/video.mp4", 120, 1024);
    }
}
