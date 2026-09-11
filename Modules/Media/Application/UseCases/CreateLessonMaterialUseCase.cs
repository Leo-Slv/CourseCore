using CourseCore.Api.Modules.AuditLogs.Application.Constants;
using CourseCore.Api.Modules.AuditLogs.Application.Services;
using CourseCore.Api.Modules.Courses.Domain.Repositories;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.Validation;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Modules.Media.Domain.Repositories;
using CourseCore.Api.Shared.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Media.Application.UseCases;

public class CreateLessonMaterialUseCase
{
    private readonly ILessonMaterialRepository _materials;
    private readonly ILessonRepository _lessons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogs;

    public CreateLessonMaterialUseCase(
        ILessonMaterialRepository materials,
        ILessonRepository lessons,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogs)
    {
        _materials = materials;
        _lessons = lessons;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
    }

    public Task<LessonMaterialOutput> ExecuteAsync(
        CreateLessonMaterialInput input,
        CancellationToken cancellationToken = default)
    {
        MaterialInputValidator.Validate(input);
        var storageProvider = ParseStorageProvider(input.StorageProvider);

        return _unitOfWork.ExecuteAsync(async () =>
        {
            var lesson = await _lessons.FindByIdAsync(input.LessonId, cancellationToken);

            if (lesson is null)
            {
                throw new NotFoundException("Lesson not found.");
            }

            var displayOrder = input.DisplayOrder
                ?? await GetNextDisplayOrderAsync(input.LessonId, cancellationToken);

            var material = LessonMaterial.Create(
                input.LessonId,
                input.Title,
                input.FileName,
                input.ContentType,
                storageProvider,
                input.StorageKey,
                input.SizeBytes,
                displayOrder);

            await _materials.CreateAsync(material, cancellationToken);
            await _auditLogs.RecordAsync(
                AuditLogActionNames.LessonMaterialCreated,
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

    private async Task<int> GetNextDisplayOrderAsync(Guid lessonId, CancellationToken cancellationToken)
    {
        var existing = await _materials.ListByLessonIdAsync(lessonId, cancellationToken);

        return existing.Count == 0 ? 0 : existing.Max(material => material.DisplayOrder) + 1;
    }

    private static MaterialStorageProvider ParseStorageProvider(string storageProvider)
    {
        if (Enum.TryParse<MaterialStorageProvider>(storageProvider, ignoreCase: true, out var provider))
        {
            return provider;
        }

        throw new ArgumentException("StorageProvider is invalid.");
    }
}
