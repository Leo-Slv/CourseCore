namespace CourseCore.Api.Modules.Media.Application.DTOs;

public class YouTubeVideoMetadata
{
    public string Title { get; init; } = string.Empty;

    public string ThumbnailUrl { get; init; } = string.Empty;

    public int DurationSeconds { get; init; }
}
