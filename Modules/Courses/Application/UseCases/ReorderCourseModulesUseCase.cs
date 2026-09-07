using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class ReorderCourseModulesUseCase
{
    private readonly ICourseRepository _courses;
    private readonly ICourseModuleRepository _courseModules;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public ReorderCourseModulesUseCase(
        ICourseRepository courses,
        ICourseModuleRepository courseModules,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _courses = courses;
        _courseModules = courseModules;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task ExecuteAsync(ReorderCourseModulesInput input, CancellationToken cancellationToken = default)
    {
        if (input.CourseId == Guid.Empty)
        {
            throw new ArgumentException("CourseId is required.", nameof(input));
        }

        if (input.OrderedModuleIds is null || input.OrderedModuleIds.Count == 0)
        {
            throw new ApplicationValidationException("OrderedModuleIds is required.");
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var course = await _courses.FindByIdAsync(input.CourseId, cancellationToken);

            if (course is null)
            {
                throw new NotFoundException("Course not found.");
            }

            var existingModules = await _courseModules.ListByCourseIdAsync(input.CourseId, cancellationToken);
            var existingIds = existingModules.Select(module => module.Id).ToHashSet();
            var requestedIds = input.OrderedModuleIds.ToHashSet();

            if (existingIds.Count != input.OrderedModuleIds.Count
                || requestedIds.Count != input.OrderedModuleIds.Count
                || !existingIds.SetEquals(requestedIds))
            {
                throw new ApplicationValidationException("OrderedModuleIds must match the course's existing modules exactly.");
            }

            await _courseModules.ReorderAsync(input.CourseId, input.OrderedModuleIds, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.CourseModuleReordered,
                "Course",
                input.CourseId,
                cancellationToken: cancellationToken);
        }, cancellationToken);
    }
}
