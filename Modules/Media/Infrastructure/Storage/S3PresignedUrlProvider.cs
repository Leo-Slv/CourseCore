using Amazon.S3;
using Amazon.S3.Model;
using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.Options;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Media.Infrastructure.Storage;

public class S3PresignedUrlProvider : IS3PresignedUrlProvider
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3StorageOptions _options;

    public S3PresignedUrlProvider(IAmazonS3 s3Client, IOptions<S3StorageOptions> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public Task<string> GeneratePresignedUploadUrlAsync(
        string storageKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        cancellationToken.ThrowIfCancellationRequested();

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = DateTime.UtcNow.AddMinutes(_options.UploadUrlExpirationMinutes)
        };

        return Task.FromResult(_s3Client.GetPreSignedURL(request));
    }

    public Task<string> GeneratePresignedDownloadUrlAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        cancellationToken.ThrowIfCancellationRequested();

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(_options.DownloadUrlExpirationMinutes)
        };

        return Task.FromResult(_s3Client.GetPreSignedURL(request));
    }

    public string GetPublicUrl(string storageKey)
    {
        EnsureConfigured();

        return $"https://{_options.BucketName}.s3.{_options.Region}.amazonaws.com/{storageKey}";
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException("S3 storage is not configured.");
        }
    }
}
