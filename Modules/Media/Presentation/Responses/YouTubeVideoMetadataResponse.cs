namespace CourseCore.Api.Modules.Media.Presentation.Responses;

public class YouTubeVideoMetadataResponse
{
    public string Title { get; init; } = string.Empty;

    public string ThumbnailUrl { get; init; } = string.Empty;

    public int DurationSeconds { get; init; }
}
