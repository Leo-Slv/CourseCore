using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Modules.Media.Application.Validation;

public static class MediaInputValidator
{
    public static void Validate(CreateVideoInput input)
    {
        if (input.LessonId == Guid.Empty
            || !IsValidRequired(input.Title, MediaValidationLimits.TitleMaxLength)
            || !IsValidOptional(input.Description, MediaValidationLimits.DescriptionMaxLength)
            || !IsValidRequired(input.StorageProvider, MediaValidationLimits.StorageProviderMaxLength)
            || !IsValidRequired(input.StorageKey, MediaValidationLimits.StorageKeyMaxLength)
            || !IsValidHttpUrl(input.ThumbnailUrl, MediaValidationLimits.ThumbnailUrlMaxLength)
            || input.DurationSeconds is < 0 or > MediaValidationLimits.MaxDurationSeconds
            || input.SizeBytes is < 0 or > MediaValidationLimits.MaxSizeBytes)
        {
            throw new ApplicationValidationException("Video payload is invalid.");
        }
    }

    private static bool IsValidRequired(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maxLength;

    private static bool IsValidOptional(string? value, int maxLength) =>
        value is null || value.Trim().Length <= maxLength;

    private static bool IsValidHttpUrl(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            && Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https";
    }
}
