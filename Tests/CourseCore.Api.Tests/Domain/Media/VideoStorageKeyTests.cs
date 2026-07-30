using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Tests.Domain.Media;

public class VideoStorageKeyTests
{
    [Theory]
    [InlineData("videos/video.mp4")]
    [InlineData("videos/2026/intro_01.mp4")]
    public void Create_WhenStorageKeyIsSafe_ShouldCreateVideo(string storageKey)
    {
        var video = Video.Create(
            Guid.NewGuid(),
            "Video",
            "Description",
            VideoStorageProvider.Local,
            storageKey,
            durationSeconds: 120,
            sizeBytes: 1024);

        Assert.Equal(storageKey, video.StorageKey);
    }

    [Theory]
    [InlineData("https://media.example/video.mp4")]
    [InlineData("../video.mp4")]
    [InlineData("videos/../video.mp4")]
    [InlineData("/videos/video.mp4")]
    [InlineData("videos\\video.mp4")]
    public void Create_WhenStorageKeyIsUnsafe_ShouldThrow(string storageKey)
    {
        Assert.Throws<DomainException>(() => Video.Create(
            Guid.NewGuid(),
            "Video",
            "Description",
            VideoStorageProvider.Local,
            storageKey,
            durationSeconds: 120,
            sizeBytes: 1024));
    }
}
