namespace CourseCore.Api.Modules.Media.Application.DTOs;

public class VideoPlaybackOutput
{
    public Guid VideoId { get; init; }

    public Guid LessonId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string PlaybackUrl { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }

    public int DurationSeconds { get; init; }

    public string Status { get; init; } = string.Empty;
}
