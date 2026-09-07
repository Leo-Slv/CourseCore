using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Application.Validation;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class UpdateCourseModuleUseCase
{
    private readonly ICourseModuleRepository _courseModules;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public UpdateCourseModuleUseCase(
        ICourseModuleRepository courseModules,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _courseModules = courseModules;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<CourseModuleOutput> ExecuteAsync(
        UpdateCourseModuleInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.ModuleId == Guid.Empty)
        {
            throw new ArgumentException("ModuleId is required.", nameof(input));
        }

        CourseInputValidator.ValidateModuleFields(input.Title, input.Description);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var module = await _courseModules.FindByIdAsync(input.ModuleId, cancellationToken);

            if (module is null)
            {
                throw new NotFoundException("Course module not found.");
            }

            module.ChangeTitle(input.Title);
            module.ChangeDescription(input.Description);

            if (input.Published != module.Published)
            {
                if (input.Published)
                {
                    module.Publish();
                }
                else
                {
                    module.Unpublish();
                }
            }

            await _courseModules.UpdateAsync(module, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.CourseModuleUpdated,
                "CourseModule",
                module.Id,
                cancellationToken: cancellationToken);

            return CourseModuleOutput.FromModule(module);
        }, cancellationToken);
    }
}
