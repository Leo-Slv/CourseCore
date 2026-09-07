using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class RemoveLessonVideoUseCase
{
    private readonly IVideoRepository _videos;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public RemoveLessonVideoUseCase(
        IVideoRepository videos,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _videos = videos;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task ExecuteAsync(Guid lessonId, CancellationToken cancellationToken = default)
    {
        if (lessonId == Guid.Empty)
        {
            throw new ArgumentException("LessonId is required.", nameof(lessonId));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var video = await _videos.FindByLessonIdAsync(lessonId, cancellationToken);

            if (video is null)
            {
                throw new NotFoundException("No video is registered for this lesson.");
            }

            await _videos.RemoveAsync(video.Id, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.VideoRemoved,
                "Video",
                video.Id,
                new Dictionary<string, string?> { ["lessonId"] = lessonId.ToString() },
                cancellationToken: cancellationToken);
        }, cancellationToken);
    }
}
