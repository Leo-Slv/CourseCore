using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class RemoveLessonMaterialUseCase
{
    private readonly ILessonMaterialRepository _materials;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public RemoveLessonMaterialUseCase(
        ILessonMaterialRepository materials,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _materials = materials;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task ExecuteAsync(Guid materialId, CancellationToken cancellationToken = default)
    {
        if (materialId == Guid.Empty)
        {
            throw new ArgumentException("MaterialId is required.", nameof(materialId));
        }

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var material = await _materials.FindByIdAsync(materialId, cancellationToken);

            if (material is null)
            {
                throw new NotFoundException("Lesson material not found.");
            }

            await _materials.RemoveAsync(material.Id, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.LessonMaterialRemoved,
                "LessonMaterial",
                material.Id,
                new Dictionary<string, string?>
                {
                    ["lessonId"] = material.LessonId.ToString(),
                    ["displayName"] = material.Title
                },
                cancellationToken: cancellationToken);
        }, cancellationToken);
    }
}
