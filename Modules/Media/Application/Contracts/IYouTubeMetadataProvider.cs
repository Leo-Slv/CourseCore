using CourseCore.Api.Modules.Media.Application.DTOs;

namespace CourseCore.Api.Modules.Media.Application.Contracts;

public interface IYouTubeMetadataProvider
{
    /// <summary>
    /// Returns null when the video id doesn't exist, is private, or is
    /// otherwise not resolvable by the YouTube Data API — never throws
    /// for a "not found" outcome specifically.
    /// </summary>
    Task<YouTubeVideoMetadata?> GetMetadataAsync(string videoId, CancellationToken cancellationToken = default);
}
