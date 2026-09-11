using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Domain.Entities;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Media.Infrastructure.Storage;

public class MaterialStorageService : IMaterialStorageService
{
    private readonly MediaPlaybackOptions _options;

    public MaterialStorageService(IOptions<MediaPlaybackOptions> options)
    {
        _options = options.Value;
        MediaPlaybackOptions.Validate(_options, requireSigningSecret: true);
    }

    public Task<string> GetUploadUrlAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("StorageKey is required.", nameof(storageKey));
        }

        var escapedStorageKey = Uri.EscapeDataString(storageKey.Trim());

        return Task.FromResult($"/media/uploads/{escapedStorageKey}");
    }

    public Task<string> GetDownloadUrlAsync(
        LessonMaterial material,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

        return Task.FromResult(url);
    }

    private string Sign(string payload)
    {
        var secretBytes = Encoding.UTF8.GetBytes(_options.SigningSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signatureBytes = HMACSHA256.HashData(secretBytes, payloadBytes);

        return Convert.ToHexString(signatureBytes).ToLowerInvariant();
    }
}
