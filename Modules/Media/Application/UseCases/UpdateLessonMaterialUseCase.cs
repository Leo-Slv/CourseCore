using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.Validation;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class UpdateLessonMaterialUseCase
{
    private readonly ILessonMaterialRepository _materials;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public UpdateLessonMaterialUseCase(
        ILessonMaterialRepository materials,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _materials = materials;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<LessonMaterialOutput> ExecuteAsync(
        UpdateLessonMaterialInput input,
        CancellationToken cancellationToken = default)
    {
        MaterialInputValidator.Validate(input);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var material = await _materials.FindByIdAsync(input.MaterialId, cancellationToken);

            if (material is null)
            {
                throw new NotFoundException("Lesson material not found.");
            }

            material.ChangeTitle(input.Title);
            material.ChangeDisplayOrder(input.DisplayOrder);

            await _materials.UpdateAsync(material, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.LessonMaterialUpdated,
                "LessonMaterial",
                material.Id,
                new Dictionary<string, string?>
                {
                    ["lessonId"] = material.LessonId.ToString(),
                    ["displayName"] = material.Title
                },
                cancellationToken: cancellationToken);

            return LessonMaterialOutput.FromMaterial(material);
        }, cancellationToken);
    }
}
