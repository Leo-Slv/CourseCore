using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.Validation;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class ReorderLessonMaterialsUseCase
{
    private readonly ILessonMaterialRepository _materials;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public ReorderLessonMaterialsUseCase(
        ILessonMaterialRepository materials,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _materials = materials;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task ExecuteAsync(ReorderLessonMaterialsInput input, CancellationToken cancellationToken = default)
    {
        MaterialInputValidator.Validate(input);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var existing = await _materials.ListByLessonIdAsync(input.LessonId, cancellationToken);
            var existingIds = existing.Select(material => material.Id).ToHashSet();
            var requestedIds = input.Items.Select(item => item.MaterialId).ToHashSet();

            if (existingIds.Count != input.Items.Count
                || requestedIds.Count != input.Items.Count
                || !existingIds.SetEquals(requestedIds))
            {
                throw new ApplicationValidationException("Items must match the lesson's existing materials exactly.");
            }

            var displayOrderByMaterialId = input.Items.ToDictionary(item => item.MaterialId, item => item.DisplayOrder);

            foreach (var material in existing)
            {
                material.ChangeDisplayOrder(displayOrderByMaterialId[material.Id]);
                await _materials.UpdateAsync(material, cancellationToken);
            }

            await _auditLogs.RecordAsync(
                AuditLogActionNames.LessonMaterialsReordered,
                "Lesson",
                input.LessonId,
                cancellationToken: cancellationToken);
        }, cancellationToken);
    }
}
