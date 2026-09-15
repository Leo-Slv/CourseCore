using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.Services;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Courses.Application.UseCases;

public class RequestCourseModuleImageUploadUseCase
{
    private const string KeyPrefix = "module-covers";

    private readonly ICourseModuleRepository _courseModules;
    private readonly ImageUploadService _imageUploadService;
    private readonly IAuditLogService _auditLogs;

    public RequestCourseModuleImageUploadUseCase(
        ICourseModuleRepository courseModules,
        ImageUploadService imageUploadService,
        IAuditLogService auditLogs)
    {
        _courseModules = courseModules;
        _imageUploadService = imageUploadService;
        _auditLogs = auditLogs;
    }

    public async Task<UploadUrlOutput> ExecuteAsync(
        Guid courseId,
        Guid moduleId,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var module = await _courseModules.FindByIdAsync(moduleId, cancellationToken);

        if (module is null || module.CourseId != courseId)
        {
            throw new NotFoundException("Course module not found.");
        }

        var output = await _imageUploadService.RequestUploadAsync(
            KeyPrefix,
            moduleId,
            fileName,
            contentType,
            sizeBytes,
            cancellationToken);

        await _auditLogs.RecordAsync(
            AuditLogActionNames.CourseModuleImageUploadRequested,
            "CourseModule",
            moduleId,
            new Dictionary<string, string?> { ["courseId"] = courseId.ToString(), ["contentType"] = contentType },
            cancellationToken: cancellationToken);

        return output;
    }
}
