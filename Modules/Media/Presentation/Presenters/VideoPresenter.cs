using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Presentation.Requests;
using CourseCore.Api.Modules.Media.Presentation.Responses;

namespace CourseCore.Api.Modules.Media.Presentation.Presenters;

public static class VideoPresenter
{
    public static CreateVideoInput ToInput(CreateVideoRequest request)
    {
        return new CreateVideoInput
        {
            LessonId = request.LessonId,
            Title = request.Title,
            Description = request.Description,
            StorageProvider = request.StorageProvider,
            StorageKey = request.StorageKey,
            PlaybackUrl = request.PlaybackUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            DurationSeconds = request.DurationSeconds,
            SizeBytes = request.SizeBytes
        };
    }

    public static RequestVideoPlaybackInput ToInput(Guid userId, RequestVideoPlaybackRequest request)
    {
        return new RequestVideoPlaybackInput
        {
            UserId = userId,
            VideoId = request.VideoId
        };
    }

    public static VideoResponse ToResponse(VideoOutput output)
    {
        return new VideoResponse
        {
            Id = output.Id,
            LessonId = output.LessonId,
            Title = output.Title,
            Description = output.Description,
            StorageProvider = output.StorageProvider,
            StorageKey = output.StorageKey,
            PlaybackUrl = output.PlaybackUrl,
            ThumbnailUrl = output.ThumbnailUrl,
            DurationSeconds = output.DurationSeconds,
            SizeBytes = output.SizeBytes,
            Status = output.Status,
            CreatedAt = output.CreatedAt,
            UpdatedAt = output.UpdatedAt
        };
    }

    public static VideoPlaybackResponse ToResponse(VideoPlaybackOutput output)
    {
        return new VideoPlaybackResponse
        {
            VideoId = output.VideoId,
            LessonId = output.LessonId,
            Title = output.Title,
            PlaybackUrl = output.PlaybackUrl,
            DurationSeconds = output.DurationSeconds,
            Status = output.Status
        };
    }
}
