using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class RemoveCourseModuleUseCase
{
    private readonly ICourseModuleRepository _courseModules;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public RemoveCourseModuleUseCase(
        ICourseModuleRepository courseModules,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _courseModules = courseModules;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task ExecuteAsync(Guid moduleId, CancellationToken cancellationToken = default)
    {
        if (moduleId == Guid.Empty)
        {
            throw new ArgumentException("ModuleId is required.", nameof(moduleId));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var module = await _courseModules.FindByIdAsync(moduleId, cancellationToken);

            if (module is null)
            {
                throw new NotFoundException("Course module not found.");
            }

            if (module.Lessons.Count > 0)
            {
                throw new ConflictException("Remove all lessons from the module before deleting it.");
            }

            await _courseModules.RemoveAsync(moduleId, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.CourseModuleDeleted,
                "CourseModule",
                moduleId,
                new Dictionary<string, string?>
                {
                    ["courseId"] = module.CourseId.ToString(),
                    ["displayName"] = module.Title
                },
                cancellationToken: cancellationToken);
        }, cancellationToken);
    }
}
