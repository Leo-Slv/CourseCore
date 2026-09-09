using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Modules.Media.Infrastructure.Persistence.Models;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Media.Infrastructure.Persistence.Mappers;

public static class VideoMapper
{
    public static Video ToDomain(VideoPersistenceModel model)
    {
        return Video.Restore(
            model.Id,
            model.LessonId,
            model.Title,
            model.Description,
            ParseStorageProvider(model.StorageProvider),
            model.StorageKey,
            model.PlaybackUrl,
            model.ThumbnailUrl,
            model.DurationSeconds,
            model.SizeBytes,
            ParseStatus(model.Status),
            ParseVisibility(model.Visibility),
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static VideoPersistenceModel ToPersistence(Video video)
    {
        return new VideoPersistenceModel
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
            Visibility = video.Visibility.ToString(),
            CreatedAt = video.CreatedAt,
            UpdatedAt = video.UpdatedAt
        };
    }

    public static void ApplyChanges(Video video, VideoPersistenceModel model)
    {
        model.LessonId = video.LessonId;
        model.Title = video.Title;
        model.Description = video.Description;
        model.StorageProvider = video.StorageProvider.ToString();
        model.StorageKey = video.StorageKey;
        model.PlaybackUrl = video.PlaybackUrl;
        model.ThumbnailUrl = video.ThumbnailUrl;
        model.DurationSeconds = video.DurationSeconds;
        model.SizeBytes = video.SizeBytes;
        model.Status = video.Status.ToString();
        model.Visibility = video.Visibility.ToString();
        model.UpdatedAt = video.UpdatedAt;
    }

    private static VideoStorageProvider ParseStorageProvider(string value)
    {
        if (Enum.TryParse<VideoStorageProvider>(value, ignoreCase: true, out var provider))
        {
            return provider;
        }

        throw new DomainException($"Invalid video storage provider: {value}.");
    }

    private static VideoStatus ParseStatus(string value)
    {
        if (Enum.TryParse<VideoStatus>(value, ignoreCase: true, out var status))
        {
            return status;
        }

        throw new DomainException($"Invalid video status: {value}.");
    }

    private static VideoVisibility ParseVisibility(string value)
    {
        if (Enum.TryParse<VideoVisibility>(value, ignoreCase: true, out var visibility))
        {
            return visibility;
        }

        throw new DomainException($"Invalid video visibility: {value}.");
    }
}
