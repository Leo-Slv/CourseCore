namespace CourseCore.Api.Modules.Media.Presentation.Responses;

public class ImageUploadUrlResponse
{
    public string StorageProvider { get; init; } = string.Empty;

    public string StorageKey { get; init; } = string.Empty;

    public string UploadUrl { get; init; } = string.Empty;

    public string PublicUrl { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }
}
