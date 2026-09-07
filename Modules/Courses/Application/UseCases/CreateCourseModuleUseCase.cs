using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Application.Validation;
using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class CreateCourseModuleUseCase
{
    private readonly ICourseRepository _courses;
    private readonly ICourseModuleRepository _courseModules;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public CreateCourseModuleUseCase(
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

    public Task<CourseModuleOutput> ExecuteAsync(
        AddCourseModuleInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.CourseId == Guid.Empty)
        {
            throw new ArgumentException("CourseId is required.", nameof(input));
        }

        CourseInputValidator.ValidateModuleFields(input.Title, input.Description);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var course = await _courses.FindByIdAsync(input.CourseId, cancellationToken);

            if (course is null)
            {
                throw new NotFoundException("Course not found.");
            }

            var existingModules = await _courseModules.ListByCourseIdAsync(input.CourseId, cancellationToken);

            if (existingModules.Count >= CourseValidationLimits.MaxModules)
            {
                throw new ConflictException("Course has reached the maximum number of modules.");
            }

            var nextDisplayOrder = existingModules.Count == 0
                ? 0
                : existingModules.Max(module => module.DisplayOrder) + 1;

            var newModule = CourseModule.Create(input.CourseId, input.Title, input.Description, nextDisplayOrder);

            await _courseModules.AddAsync(newModule, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.CourseModuleCreated,
                "CourseModule",
                newModule.Id,
                new Dictionary<string, string?> { ["courseId"] = input.CourseId.ToString() },
                cancellationToken: cancellationToken);

            return CourseModuleOutput.FromModule(newModule);
        }, cancellationToken);
    }
}
