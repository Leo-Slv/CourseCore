namespace CourseCore.Api.Modules.Media.Application.Options;

public sealed class S3StorageOptions
{
    public const string SectionName = "Media:S3";

    public string BucketName { get; init; } = string.Empty;

    public string Region { get; init; } = string.Empty;

    public string? AccessKeyId { get; init; }

    public string? SecretAccessKey { get; init; }

    public int UploadUrlExpirationMinutes { get; init; } = 15;

    public int DownloadUrlExpirationMinutes { get; init; } = 10;

    public int ImageUrlExpirationMinutes { get; init; } = 60;

    public static void Validate(S3StorageOptions options, bool required)
    {
        if (!required)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.BucketName))
        {
            throw new InvalidOperationException("S3 storage bucket name is required when S3 is an allowed storage provider.");
        }

        if (string.IsNullOrWhiteSpace(options.Region))
        {
            throw new InvalidOperationException("S3 storage region is required when S3 is an allowed storage provider.");
        }

        if (string.IsNullOrWhiteSpace(options.AccessKeyId) != string.IsNullOrWhiteSpace(options.SecretAccessKey))
        {
            throw new InvalidOperationException("S3 storage AccessKeyId and SecretAccessKey must both be set or both be empty.");
        }

        if (options.UploadUrlExpirationMinutes is < 1 or > 60)
        {
            throw new InvalidOperationException("S3 storage upload URL expiration must be between 1 and 60 minutes.");
        }

        if (options.DownloadUrlExpirationMinutes is < 1 or > 60)
        {
            throw new InvalidOperationException("S3 storage download URL expiration must be between 1 and 60 minutes.");
        }

        if (options.ImageUrlExpirationMinutes is < 1 or > 1440)
        {
            throw new InvalidOperationException("S3 storage image URL expiration must be between 1 and 1440 minutes.");
        }
    }
}
