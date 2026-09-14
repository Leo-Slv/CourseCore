namespace CourseCore.Api.Modules.Media.Application.DTOs;

public class ImageUploadUrlOutput
{
    public string StorageProvider { get; init; } = string.Empty;

    public string StorageKey { get; init; } = string.Empty;

    public string UploadUrl { get; init; } = string.Empty;

    public string PublicUrl { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }
}
