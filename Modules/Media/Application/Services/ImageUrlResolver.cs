using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.Options;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Media.Application.Services;

public class ImageUrlResolver
{
    private readonly IS3PresignedUrlProvider _s3;
    private readonly S3StorageOptions _s3Options;

    public ImageUrlResolver(IS3PresignedUrlProvider s3, IOptions<S3StorageOptions> s3Options)
    {
        _s3 = s3;
        _s3Options = s3Options.Value;
    }

    public Task<string?> ResolveAsync(string? storedValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return Task.FromResult<string?>(null);
        }

        // A value containing "://" is a full URL already — either a legacy
        // manually-pasted external link (these fields used to be free-text
        // URL inputs before uploads existed) or some future non-S3 source.
        // Only a bare internal storage key (e.g. "course-thumbnails/{id}/{guid}.jpg")
        // needs signing.
        if (storedValue.Contains("://"))
        {
            return Task.FromResult<string?>(storedValue);
        }

        return ResolveKeyAsync(storedValue, cancellationToken);
    }

    private async Task<string?> ResolveKeyAsync(string storageKey, CancellationToken cancellationToken)
    {
        return await _s3.GeneratePresignedDownloadUrlAsync(
            storageKey,
            TimeSpan.FromMinutes(_s3Options.ImageUrlExpirationMinutes),
            cancellationToken);
    }
}
