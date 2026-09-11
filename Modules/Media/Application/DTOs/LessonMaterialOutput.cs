using CourseCore.Api.Modules.Media.Domain.Entities;

namespace CourseCore.Api.Modules.Media.Application.DTOs;

public class LessonMaterialOutput
{
    public Guid Id { get; init; }

    public Guid LessonId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public string StorageProvider { get; init; } = string.Empty;

    public string StorageKey { get; init; } = string.Empty;

    public long SizeBytes { get; init; }

    public int DisplayOrder { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public static LessonMaterialOutput FromMaterial(LessonMaterial material)
    {
        return new LessonMaterialOutput
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
}
