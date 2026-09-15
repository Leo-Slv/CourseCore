using CourseCore.Api.Modules.Access.Domain.Repositories;
using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.Services;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Access.Application.UseCases;

public class RequestAreaImageUploadUseCase
{
    private const string KeyPrefix = "area-covers";

    private readonly IAreaRepository _areas;
    private readonly ImageUploadService _imageUploadService;
    private readonly IAuditLogService _auditLogs;

    public RequestAreaImageUploadUseCase(
        IAreaRepository areas,
        ImageUploadService imageUploadService,
        IAuditLogService auditLogs)
    {
        _areas = areas;
        _imageUploadService = imageUploadService;
        _auditLogs = auditLogs;
    }

    public async Task<UploadUrlOutput> ExecuteAsync(
        Guid areaId,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        var area = await _areas.FindByIdAsync(areaId, cancellationToken);

        if (area is null)
        {
            throw new NotFoundException("Area not found.");
        }

        var output = await _imageUploadService.RequestUploadAsync(
            KeyPrefix,
            areaId,
            fileName,
            contentType,
            sizeBytes,
            cancellationToken);

        await _auditLogs.RecordAsync(
            AuditLogActionNames.AreaImageUploadRequested,
            "Area",
            areaId,
            new Dictionary<string, string?> { ["contentType"] = contentType },
            cancellationToken: cancellationToken);

        return output;
    }
}
