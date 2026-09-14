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

    public const int MaterialFileNameMaxLength = 255;
    public const int MaterialContentTypeMaxLength = 150;
    public const long MaxMaterialSizeBytes = 50L * 1024 * 1024;

    public static readonly IReadOnlySet<string> AllowedVideoContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4",
        "video/quicktime",
        "video/webm",
        "video/x-matroska"
    };

    public static readonly IReadOnlySet<string> AllowedMaterialContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/zip",
        "image/png",
        "image/jpeg"
    };
}
