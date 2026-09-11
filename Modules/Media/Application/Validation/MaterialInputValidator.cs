using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Media.Application.Validation;

public static class MaterialInputValidator
{
    public static void Validate(CreateLessonMaterialInput input)
    {
        if (input.LessonId == Guid.Empty
            || !IsValidRequired(input.Title, MediaValidationLimits.TitleMaxLength)
            || !IsValidRequired(input.FileName, MediaValidationLimits.MaterialFileNameMaxLength)
            || !IsValidRequired(input.ContentType, MediaValidationLimits.MaterialContentTypeMaxLength)
            || !IsValidRequired(input.StorageProvider, MediaValidationLimits.StorageProviderMaxLength)
            || !IsValidRequired(input.StorageKey, MediaValidationLimits.StorageKeyMaxLength)
            || input.SizeBytes is < 0 or > MediaValidationLimits.MaxMaterialSizeBytes
            || input.DisplayOrder is < 0)
        {
            throw new ApplicationValidationException("Material payload is invalid.");
        }
    }

    public static void Validate(UpdateLessonMaterialInput input)
    {
        if (input.MaterialId == Guid.Empty
            || !IsValidRequired(input.Title, MediaValidationLimits.TitleMaxLength)
            || input.DisplayOrder < 0)
        {
            throw new ApplicationValidationException("Material payload is invalid.");
        }
    }

    public static void Validate(ReorderLessonMaterialsInput input)
    {
        if (input.LessonId == Guid.Empty
            || input.Items.Count == 0
            || input.Items.Any(item => item.MaterialId == Guid.Empty || item.DisplayOrder < 0)
            || input.Items.Select(item => item.MaterialId).Distinct().Count() != input.Items.Count)
        {
            throw new ApplicationValidationException("Reorder payload is invalid.");
        }
    }

    private static bool IsValidRequired(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maxLength;
}
