using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class ReorderLessonsUseCase
{
    private readonly ICourseModuleRepository _courseModules;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public ReorderLessonsUseCase(
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

    public Task ExecuteAsync(ReorderLessonsInput input, CancellationToken cancellationToken = default)
    {
        if (input.ModuleId == Guid.Empty)
        {
            throw new ArgumentException("ModuleId is required.", nameof(input));
        }

        if (input.OrderedLessonIds is null || input.OrderedLessonIds.Count == 0)
        {
            throw new ApplicationValidationException("OrderedLessonIds is required.");
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var module = await _courseModules.FindByIdAsync(input.ModuleId, cancellationToken);

            if (module is null)
            {
                throw new NotFoundException("Course module not found.");
            }

            var existingIds = module.Lessons.Select(lesson => lesson.Id).ToHashSet();
            var requestedIds = input.OrderedLessonIds.ToHashSet();

            if (existingIds.Count != input.OrderedLessonIds.Count
                || requestedIds.Count != input.OrderedLessonIds.Count
                || !existingIds.SetEquals(requestedIds))
            {
                throw new ApplicationValidationException("OrderedLessonIds must match the module's existing lessons exactly.");
            }

            await _lessons.ReorderAsync(input.ModuleId, input.OrderedLessonIds, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.LessonReordered,
                "CourseModule",
                input.ModuleId,
                new Dictionary<string, string?> { ["displayName"] = module.Title },
                cancellationToken: cancellationToken);
        }, cancellationToken);
    }
}
