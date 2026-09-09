using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Application.Validation;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class MoveLessonUseCase
{
    private readonly ILessonRepository _lessons;
    private readonly ICourseModuleRepository _courseModules;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public MoveLessonUseCase(
        ILessonRepository lessons,
        ICourseModuleRepository courseModules,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _lessons = lessons;
        _courseModules = courseModules;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<LessonOutput> ExecuteAsync(
        MoveLessonInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.LessonId == Guid.Empty)
        {
            throw new ArgumentException("LessonId is required.", nameof(input));
        }

        if (input.TargetModuleId == Guid.Empty)
        {
            throw new ArgumentException("TargetModuleId is required.", nameof(input));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var lesson = await _lessons.FindByIdAsync(input.LessonId, cancellationToken);

            if (lesson is null)
            {
                throw new NotFoundException("Lesson not found.");
            }

            if (lesson.ModuleId == input.TargetModuleId)
            {
                return LessonOutput.FromLesson(lesson, videoId: null, durationSeconds: null);
            }

            var currentModule = await _courseModules.FindByIdAsync(lesson.ModuleId, cancellationToken);

            if (currentModule is null)
            {
                throw new NotFoundException("Course module not found.");
            }

            var targetModule = await _courseModules.FindByIdAsync(input.TargetModuleId, cancellationToken);

            if (targetModule is null)
            {
                throw new NotFoundException("Target course module not found.");
            }

            if (targetModule.CourseId != currentModule.CourseId)
            {
                throw new ApplicationValidationException("TargetModuleId must belong to the same course.");
            }

            var targetLessons = await _lessons.ListByModuleIdAsync(input.TargetModuleId, cancellationToken);

            if (targetLessons.Count >= CourseValidationLimits.MaxLessonsPerModule)
            {
                throw new ConflictException("Target module has reached the maximum number of lessons.");
            }

            var nextDisplayOrder = targetLessons.Count == 0
                ? 0
                : targetLessons.Max(l => l.DisplayOrder) + 1;

            lesson.ChangeModuleId(input.TargetModuleId);
            lesson.ChangeDisplayOrder(nextDisplayOrder);

            await _lessons.UpdateAsync(lesson, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.LessonMoved,
                "Lesson",
                lesson.Id,
                new Dictionary<string, string?>
                {
                    ["fromModuleId"] = currentModule.Id.ToString(),
                    ["toModuleId"] = targetModule.Id.ToString(),
                    ["displayName"] = lesson.Title
                },
                cancellationToken: cancellationToken);

            return LessonOutput.FromLesson(lesson, videoId: null, durationSeconds: null);
        }, cancellationToken);
    }
}
