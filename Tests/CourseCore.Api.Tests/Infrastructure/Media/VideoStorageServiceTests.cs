using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Modules.Media.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Infrastructure.Media;

public class VideoStorageServiceTests
{
    [Fact]
    public async Task GeneratePlaybackUrlAsync_WhenConfigurationIsValid_ShouldReturnSignedTemporaryUrl()
    {
        var service = CreateService();
        var video = CreateReadyVideo();
        var userId = Guid.NewGuid();

        var result = await service.GeneratePlaybackUrlAsync(video, userId);

        Assert.Contains($"/videos/{video.Id}/playback", result.Url);
        Assert.Contains("expires=", result.Url);
        Assert.Contains("signature=", result.Url);
        Assert.DoesNotContain(video.StorageKey, result.Url);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_WhenSigningSecretIsUnsafe_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() => CreateService(signingSecret: "CHANGE_ME"));
    }

    [Fact]
    public async Task GeneratePlaybackUrlAsync_WhenProviderIsNotAllowed_ShouldThrow()
    {
        var service = CreateService(allowedProviders: ["S3"]);
        var video = CreateReadyVideo();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GeneratePlaybackUrlAsync(video, Guid.NewGuid()));
    }

    private static VideoStorageService CreateService(
        string signingSecret = "test-media-signing-secret-with-at-least-32-characters",
        IReadOnlyCollection<string>? allowedProviders = null)
    {
        return new VideoStorageService(Options.Create(new MediaPlaybackOptions
        {
            SigningSecret = signingSecret,
            BaseUrl = "/media",
            SignedUrlExpirationMinutes = 10,
            AllowedStorageProviders = allowedProviders ?? ["Local"]
        }));
    }

    private static Video CreateReadyVideo()
    {
        var video = Video.Create(
            Guid.NewGuid(),
            "Video",
            "Description",
            VideoStorageProvider.Local,
            "videos/video.mp4",
            durationSeconds: 120,
            sizeBytes: 1024);
        video.MarkAsReady();

        return video;
    }
}
