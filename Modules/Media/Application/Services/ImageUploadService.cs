using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Application.Validation;
using CourseCore.Api.Shared.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Media.Application.Services;

public class ImageUploadService
{
    private readonly IS3PresignedUrlProvider _s3;
    private readonly MediaPlaybackOptions _playbackOptions;
    private readonly S3StorageOptions _s3Options;

    public ImageUploadService(
        IS3PresignedUrlProvider s3,
        IOptions<MediaPlaybackOptions> playbackOptions,
        IOptions<S3StorageOptions> s3Options)
    {
        _s3 = s3;
        _playbackOptions = playbackOptions.Value;
        _s3Options = s3Options.Value;
    }

    public async Task<ImageUploadUrlOutput> RequestUploadAsync(
        string keyPrefix,
        Guid ownerId,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        Validate(fileName, contentType, sizeBytes);

        if (!_playbackOptions.AllowedStorageProviders.Any(allowed =>
            string.Equals(allowed.Trim(), "S3", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ApplicationValidationException("Storage provider is not allowed.");
        }

        var storageKey = StorageKeyGenerator.Generate(keyPrefix, ownerId, fileName);
        var uploadUrl = await _s3.GeneratePresignedUploadUrlAsync(storageKey, contentType, cancellationToken);
        var publicUrl = _s3.GetPublicUrl(storageKey);

        return new ImageUploadUrlOutput
        {
            StorageProvider = "S3",
            StorageKey = storageKey,
            UploadUrl = uploadUrl,
            PublicUrl = publicUrl,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_s3Options.UploadUrlExpirationMinutes)
        };
    }

    private static void Validate(string fileName, string contentType, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || fileName.Trim().Length > MediaValidationLimits.ImageFileNameMaxLength
            || string.IsNullOrWhiteSpace(contentType)
            || !MediaValidationLimits.AllowedImageContentTypes.Contains(contentType.Trim())
            || sizeBytes is <= 0 or > MediaValidationLimits.MaxImageSizeBytes)
        {
            throw new ApplicationValidationException("Image upload request payload is invalid.");
        }
    }
}
