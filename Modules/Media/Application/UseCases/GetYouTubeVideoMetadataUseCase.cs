using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class GetYouTubeVideoMetadataUseCase
{
    private readonly IYouTubeMetadataProvider _youTubeMetadataProvider;

    public GetYouTubeVideoMetadataUseCase(IYouTubeMetadataProvider youTubeMetadataProvider)
    {
        _youTubeMetadataProvider = youTubeMetadataProvider;
    }

    public async Task<YouTubeVideoMetadata> ExecuteAsync(string videoId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new ArgumentException("VideoId is required.", nameof(videoId));
        }

        var metadata = await _youTubeMetadataProvider.GetMetadataAsync(videoId.Trim(), cancellationToken);

        if (metadata is null)
        {
            throw new NotFoundException("YouTube video not found.");
        }

        return metadata;
    }
}
