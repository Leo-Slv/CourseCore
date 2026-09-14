using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Domain.Entities;
using CourseCore.Api.Modules.Media.Domain.Enums;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Media.Infrastructure.Storage;

public class MaterialStorageService : IMaterialStorageService
{
    private readonly MediaPlaybackOptions _options;
    private readonly IS3PresignedUrlProvider _s3PresignedUrlProvider;

    public MaterialStorageService(IOptions<MediaPlaybackOptions> options, IS3PresignedUrlProvider s3PresignedUrlProvider)
    {
        _options = options.Value;
        _s3PresignedUrlProvider = s3PresignedUrlProvider;
        MediaPlaybackOptions.Validate(_options, requireSigningSecret: true);
    }

    public Task<string> GetUploadUrlAsync(
        MaterialStorageProvider provider,
        string storageKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("StorageKey is required.", nameof(storageKey));
        }

        if (provider == MaterialStorageProvider.S3)
        {
            return _s3PresignedUrlProvider.GeneratePresignedUploadUrlAsync(storageKey.Trim(), contentType, cancellationToken);
        }

        var escapedStorageKey = Uri.EscapeDataString(storageKey.Trim());

        return Task.FromResult($"/media/uploads/{escapedStorageKey}");
    }

    public async Task<string> GetDownloadUrlAsync(
        LessonMaterial material,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (material.StorageProvider == MaterialStorageProvider.S3)
        {
            return await _s3PresignedUrlProvider.GeneratePresignedDownloadUrlAsync(material.StorageKey, cancellationToken);
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_options.SignedUrlExpirationMinutes);
        var expiresUnixTime = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();
        var signaturePayload = string.Join(
            "\n",
            material.Id.ToString("N"),
            material.StorageProvider.ToString(),
            material.StorageKey,
            expiresUnixTime.ToString(CultureInfo.InvariantCulture));
        var signature = Sign(signaturePayload);
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var escapedMaterialId = Uri.EscapeDataString(material.Id.ToString());
        var url = $"{baseUrl}/materials/{escapedMaterialId}/download"
            + $"?expires={expiresUnixTime.ToString(CultureInfo.InvariantCulture)}"
            + $"&signature={Uri.EscapeDataString(signature)}";

        return url;
    }

    private string Sign(string payload)
    {
        var secretBytes = Encoding.UTF8.GetBytes(_options.SigningSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signatureBytes = HMACSHA256.HashData(secretBytes, payloadBytes);

        return Convert.ToHexString(signatureBytes).ToLowerInvariant();
    }
}
