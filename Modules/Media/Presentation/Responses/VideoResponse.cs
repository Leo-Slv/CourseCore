namespace CourseCore.Api.Modules.Media.Presentation.Responses;

public class VideoResponse
{
    public Guid Id { get; init; }

    public Guid LessonId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string StorageProvider { get; init; } = string.Empty;

    public string StorageKey { get; init; } = string.Empty;

    public string? PlaybackUrl { get; init; }

    public string? ThumbnailUrl { get; init; }

    public int DurationSeconds { get; init; }

    public long SizeBytes { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
