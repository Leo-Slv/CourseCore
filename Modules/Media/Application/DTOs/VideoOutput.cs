using CourseCore.Api.Modules.Media.Domain.Entities;

namespace CourseCore.Api.Modules.Media.Application.DTOs;

public class VideoOutput
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

    public static VideoOutput FromVideo(Video video)
    {
        return new VideoOutput
        {
            Id = video.Id,
            LessonId = video.LessonId,
            Title = video.Title,
            Description = video.Description,
            StorageProvider = video.StorageProvider.ToString(),
            StorageKey = video.StorageKey,
            PlaybackUrl = video.PlaybackUrl,
            ThumbnailUrl = video.ThumbnailUrl,
            DurationSeconds = video.DurationSeconds,
            SizeBytes = video.SizeBytes,
            Status = video.Status.ToString(),
            CreatedAt = video.CreatedAt,
            UpdatedAt = video.UpdatedAt
        };
    }
}
