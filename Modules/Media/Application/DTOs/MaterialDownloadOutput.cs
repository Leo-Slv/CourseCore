namespace CourseCore.Api.Modules.Media.Application.DTOs;

public class MaterialDownloadOutput
{
    public Guid MaterialId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string DownloadUrl { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }
}
