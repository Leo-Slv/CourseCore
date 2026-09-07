using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Application.Validation;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class CreateLessonUseCase
{
    private readonly ICourseModuleRepository _courseModules;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public CreateLessonUseCase(
        ICourseModuleRepository courseModules,
        ILessonRepository lessons,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _courseModules = courseModules;
        _lessons = lessons;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<LessonOutput> ExecuteAsync(
        AddLessonInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.ModuleId == Guid.Empty)
        {
            throw new ArgumentException("ModuleId is required.", nameof(input));
        }

        CourseInputValidator.ValidateLessonFields(input.Title, input.Description);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var module = await _courseModules.FindByIdAsync(input.ModuleId, cancellationToken);

            if (module is null)
            {
                throw new NotFoundException("Course module not found.");
            }

            if (module.Lessons.Count >= CourseValidationLimits.MaxLessonsPerModule)
            {
                throw new ConflictException("Module has reached the maximum number of lessons.");
            }

            var nextDisplayOrder = module.Lessons.Count == 0
                ? 0
                : module.Lessons.Max(lesson => lesson.DisplayOrder) + 1;

            var newLesson = Lesson.Create(input.ModuleId, input.Title, input.Description, nextDisplayOrder);

            if (input.FreePreview)
            {
                newLesson.MarkAsFreePreview();
            }

            await _lessons.AddAsync(newLesson, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.LessonCreated,
                "Lesson",
                newLesson.Id,
                new Dictionary<string, string?> { ["moduleId"] = input.ModuleId.ToString() },
                cancellationToken: cancellationToken);

            return LessonOutput.FromLesson(newLesson, videoId: null, durationSeconds: null);
        }, cancellationToken);
    }
}
