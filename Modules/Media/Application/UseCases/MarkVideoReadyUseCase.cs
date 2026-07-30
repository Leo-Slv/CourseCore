using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class MarkVideoReadyUseCase
{
    private readonly IVideoRepository _videos;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public MarkVideoReadyUseCase(
        IVideoRepository videos,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _videos = videos;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<VideoOutput> ExecuteAsync(
        MarkVideoReadyInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.VideoId == Guid.Empty)
        {
            throw new ArgumentException("VideoId is required.", nameof(input));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var video = await _videos.FindByIdAsync(input.VideoId, cancellationToken);

            if (video is null)
            {
                throw new NotFoundException("Video not found.");
            }

            if (video.DurationSeconds <= 0)
            {
                throw new ConflictException("Video duration must be greater than zero before marking it as ready.");
            }

            video.MarkAsReady();

            await _videos.UpdateAsync(video, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.VideoMarkedReady,
                "Video",
                video.Id,
                new Dictionary<string, string?>
                {
                    ["lessonId"] = video.LessonId.ToString(),
                    ["storageProvider"] = video.StorageProvider.ToString(),
                    ["status"] = video.Status.ToString()
                },
                cancellationToken: cancellationToken);

            return VideoOutput.FromVideo(video);
        }, cancellationToken);
    }
}
