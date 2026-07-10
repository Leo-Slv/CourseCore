using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Domain.Entities;

namespace CourseCore.Api.Modules.Media.Infrastructure.Storage;

public class VideoStorageService : IVideoStorageService
{
    public Task<string> GeneratePlaybackUrlAsync(
        Video video,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(video.PlaybackUrl))
        {
            return Task.FromResult(video.PlaybackUrl);
        }

        var videoId = Uri.EscapeDataString(video.Id.ToString());

        return Task.FromResult($"/media/videos/{videoId}/playback");
    }

    public Task<string> GetUploadUrlAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("StorageKey is required.", nameof(storageKey));
        }

        var escapedStorageKey = Uri.EscapeDataString(storageKey.Trim());

        return Task.FromResult($"/media/uploads/{escapedStorageKey}");
    }
}
