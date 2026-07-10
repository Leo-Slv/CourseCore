using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Media.Domain.Entities;

public class Video : EntityBase
{
    private Video(
        Guid lessonId,
        string title,
        string description,
        VideoStorageProvider storageProvider,
        string storageKey,
        string? playbackUrl,
        string? thumbnailUrl,
        int durationSeconds,
        long sizeBytes,
        VideoStatus status)
    {
        LessonId = ValidateId(lessonId, nameof(LessonId));
        Title = ValidateRequired(title, nameof(Title));
        Description = NormalizeDescription(description);
        StorageProvider = storageProvider;
        StorageKey = ValidateRequired(storageKey, nameof(StorageKey));
        PlaybackUrl = NormalizeOptional(playbackUrl);
        ThumbnailUrl = NormalizeOptional(thumbnailUrl);
        DurationSeconds = ValidateNonNegative(durationSeconds, nameof(DurationSeconds));
        SizeBytes = ValidateNonNegative(sizeBytes, nameof(SizeBytes));
        Status = status;
    }

    public Guid LessonId { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public VideoStorageProvider StorageProvider { get; private set; }

    public string StorageKey { get; private set; }

    public string? PlaybackUrl { get; private set; }

    public string? ThumbnailUrl { get; private set; }

    public int DurationSeconds { get; private set; }

    public long SizeBytes { get; private set; }

    public VideoStatus Status { get; private set; }

    public static Video Create(
        Guid lessonId,
        string title,
        string description,
        VideoStorageProvider storageProvider,
        string storageKey,
        int durationSeconds,
        long sizeBytes,
        string? thumbnailUrl = null)
    {
        return new Video(
            lessonId,
            title,
            description,
            storageProvider,
            storageKey,
            playbackUrl: null,
            thumbnailUrl,
            durationSeconds,
            sizeBytes,
            VideoStatus.Processing);
    }

    public static Video Restore(
        Guid id,
        Guid lessonId,
        string title,
        string description,
        VideoStorageProvider storageProvider,
        string storageKey,
        string? playbackUrl,
        string? thumbnailUrl,
        int durationSeconds,
        long sizeBytes,
        VideoStatus status,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new Video(
            lessonId,
            title,
            description,
            storageProvider,
            storageKey,
            playbackUrl,
            thumbnailUrl,
            durationSeconds,
            sizeBytes,
            status)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void ChangeTitle(string title)
    {
        Title = ValidateRequired(title, nameof(Title));
        MarkAsUpdated();
    }

    public void ChangeDescription(string description)
    {
        Description = NormalizeDescription(description);
        MarkAsUpdated();
    }

    public void ChangeThumbnailUrl(string? thumbnailUrl)
    {
        ThumbnailUrl = NormalizeOptional(thumbnailUrl);
        MarkAsUpdated();
    }

    public void ChangeStorage(VideoStorageProvider provider, string storageKey)
    {
        StorageProvider = provider;
        StorageKey = ValidateRequired(storageKey, nameof(StorageKey));
        MarkAsUpdated();
    }

    public void ChangeDuration(int durationSeconds)
    {
        DurationSeconds = ValidateNonNegative(durationSeconds, nameof(DurationSeconds));
        MarkAsUpdated();
    }

    public void ChangeSize(long sizeBytes)
    {
        SizeBytes = ValidateNonNegative(sizeBytes, nameof(SizeBytes));
        MarkAsUpdated();
    }

    public void MarkAsProcessing()
    {
        Status = VideoStatus.Processing;
        MarkAsUpdated();
    }

    public void MarkAsReady(string playbackUrl)
    {
        PlaybackUrl = ValidateRequired(playbackUrl, nameof(PlaybackUrl));
        Status = VideoStatus.Ready;
        MarkAsUpdated();
    }

    public void MarkAsFailed()
    {
        Status = VideoStatus.Failed;
        MarkAsUpdated();
    }

    private static Guid ValidateId(Guid id, string fieldName)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return id;
    }

    private static string ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string NormalizeDescription(string description)
    {
        return description?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int ValidateNonNegative(int value, string fieldName)
    {
        if (value < 0)
        {
            throw new DomainException($"{fieldName} cannot be negative.");
        }

        return value;
    }

    private static long ValidateNonNegative(long value, string fieldName)
    {
        if (value < 0)
        {
            throw new DomainException($"{fieldName} cannot be negative.");
        }

        return value;
    }
}
