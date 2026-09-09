using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class ActivateVideoUseCase
{
    private readonly IVideoRepository _videos;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public ActivateVideoUseCase(IVideoRepository videos, IUnitOfWork unitOfWork, IAuditLogService auditLogs)
    {
        _videos = videos;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<VideoOutput> ExecuteAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        if (videoId == Guid.Empty)
        {
            throw new ArgumentException("VideoId is required.", nameof(videoId));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var video = await _videos.FindByIdAsync(videoId, cancellationToken);

            if (video is null)
            {
                throw new NotFoundException("Video not found.");
            }

            video.MarkAsActive();

            await _videos.UpdateAsync(video, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.VideoActivated,
                "Video",
                video.Id,
                new Dictionary<string, string?> { ["displayName"] = video.Title },
                cancellationToken: cancellationToken);

            return VideoOutput.FromVideo(video);
        }, cancellationToken);
    }
}
