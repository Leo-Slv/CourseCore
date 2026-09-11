using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Modules.Media.Infrastructure.Persistence.Models;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Media.Infrastructure.Persistence.Mappers;

public static class LessonMaterialMapper
{
    public static LessonMaterial ToDomain(LessonMaterialPersistenceModel model)
    {
        return LessonMaterial.Restore(
            model.Id,
            model.LessonId,
            model.Title,
            model.FileName,
            model.ContentType,
            ParseStorageProvider(model.StorageProvider),
            model.StorageKey,
            model.SizeBytes,
            model.DisplayOrder,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static LessonMaterialPersistenceModel ToPersistence(LessonMaterial material)
    {
        return new LessonMaterialPersistenceModel
        {
            Id = material.Id,
            LessonId = material.LessonId,
            Title = material.Title,
            FileName = material.FileName,
            ContentType = material.ContentType,
            StorageProvider = material.StorageProvider.ToString(),
            StorageKey = material.StorageKey,
            SizeBytes = material.SizeBytes,
            DisplayOrder = material.DisplayOrder,
            CreatedAt = material.CreatedAt,
            UpdatedAt = material.UpdatedAt
        };
    }

    public static void ApplyChanges(LessonMaterial material, LessonMaterialPersistenceModel model)
    {
        model.LessonId = material.LessonId;
        model.Title = material.Title;
        model.FileName = material.FileName;
        model.ContentType = material.ContentType;
        model.StorageProvider = material.StorageProvider.ToString();
        model.StorageKey = material.StorageKey;
        model.SizeBytes = material.SizeBytes;
        model.DisplayOrder = material.DisplayOrder;
        model.UpdatedAt = material.UpdatedAt;
    }

    private static MaterialStorageProvider ParseStorageProvider(string value)
    {
        if (Enum.TryParse<MaterialStorageProvider>(value, ignoreCase: true, out var provider))
        {
            return provider;
        }

        throw new DomainException($"Invalid material storage provider: {value}.");
    }
}
