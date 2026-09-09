using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Modules.Progress.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class RemoveLessonUseCase
{
    private readonly ILessonRepository _lessons;
    private readonly IVideoRepository _videos;
    private readonly IProgressRepository _progress;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public RemoveLessonUseCase(
        ILessonRepository lessons,
        IVideoRepository videos,
        IProgressRepository progress,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _lessons = lessons;
        _videos = videos;
        _progress = progress;
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
            var lesson = await _lessons.FindByIdAsync(lessonId, cancellationToken);

            if (lesson is null)
            {
                throw new NotFoundException("Lesson not found.");
            }

            if (await _progress.ExistsAnyForLessonAsync(lessonId, cancellationToken))
            {
                throw new ConflictException("Lesson has recorded student progress and cannot be removed.");
            }

            var video = await _videos.FindByLessonIdAsync(lessonId, cancellationToken);

            if (video is not null)
            {
                await _videos.RemoveAsync(video.Id, cancellationToken);
            }

            await _lessons.RemoveAsync(lessonId, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.LessonDeleted,
                "Lesson",
                lessonId,
                new Dictionary<string, string?>
                {
                    ["moduleId"] = lesson.ModuleId.ToString(),
                    ["displayName"] = lesson.Title
                },
                cancellationToken: cancellationToken);
        }, cancellationToken);
    }
}
