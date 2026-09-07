using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Application.Validation;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class UpdateLessonUseCase
{
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public UpdateLessonUseCase(
        ILessonRepository lessons,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _lessons = lessons;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<LessonOutput> ExecuteAsync(
        UpdateLessonInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.LessonId == Guid.Empty)
        {
            throw new ArgumentException("LessonId is required.", nameof(input));
        }

        CourseInputValidator.ValidateLessonFields(input.Title, input.Description);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var lesson = await _lessons.FindByIdAsync(input.LessonId, cancellationToken);

            if (lesson is null)
            {
                throw new NotFoundException("Lesson not found.");
            }

            lesson.ChangeTitle(input.Title);
            lesson.ChangeDescription(input.Description);

            if (input.FreePreview != lesson.FreePreview)
            {
                if (input.FreePreview)
                {
                    lesson.MarkAsFreePreview();
                }
                else
                {
                    lesson.RemoveFreePreview();
                }
            }

            if (input.Published != lesson.Published)
            {
                if (input.Published)
                {
                    lesson.Publish();
                }
                else
                {
                    lesson.Unpublish();
                }
            }

            await _lessons.UpdateAsync(lesson, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.LessonUpdated,
                "Lesson",
                lesson.Id,
                cancellationToken: cancellationToken);

            return LessonOutput.FromLesson(lesson, videoId: null, durationSeconds: null);
        }, cancellationToken);
    }
}
