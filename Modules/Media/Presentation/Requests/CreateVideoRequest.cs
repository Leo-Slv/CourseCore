namespace CourseCore.Api.Modules.Media.Presentation.Requests;

public class CreateVideoRequest
{
    public Guid LessonId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string StorageProvider { get; init; } = string.Empty;

    public string StorageKey { get; init; } = string.Empty;

    public string? PlaybackUrl { get; init; }

    public string? ThumbnailUrl { get; init; }

    public int DurationSeconds { get; init; }

    public long SizeBytes { get; init; }
}
