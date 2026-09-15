namespace CourseCore.Api.Modules.Media.Application.Contracts;

public interface IS3PresignedUrlProvider
{
    Task<string> GeneratePresignedUploadUrlAsync(
        string storageKey,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<string> GeneratePresignedDownloadUrlAsync(
        string storageKey,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default);
}
