namespace CourseCore.Api.Modules.Media.Application.Validation;

public static class MediaValidationLimits
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 1000;
    public const int StorageProviderMaxLength = 50;
    public const int StorageKeyMaxLength = 1000;
    public const int ThumbnailUrlMaxLength = 1000;
    public const int MaxDurationSeconds = 86_400;
    public const long MaxSizeBytes = 100L * 1024 * 1024 * 1024;
}
