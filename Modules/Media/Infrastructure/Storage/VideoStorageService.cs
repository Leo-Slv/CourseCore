using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CourseCore.Api.Modules.Media.Application.Contracts;
using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Application.Options;
using CourseCore.Api.Modules.Media.Domain.Entities;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Modules.Media.Infrastructure.Storage;

public class VideoStorageService : IVideoStorageService
{
    private readonly MediaPlaybackOptions _options;

    public VideoStorageService(IOptions<MediaPlaybackOptions> options)
    {
        _options = options.Value;
        MediaPlaybackOptions.Validate(_options, requireSigningSecret: true);
    }

    public Task<VideoPlaybackUrl> GeneratePlaybackUrlAsync(
        Video video,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateAllowedStorageProvider(video);

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_options.SignedUrlExpirationMinutes);
        var expiresUnixTime = new DateTimeOffset(expiresAt).ToUnixTimeSeconds();
        var signaturePayload = string.Join(
            "\n",
            video.Id.ToString("N"),
            userId.ToString("N"),
            video.StorageProvider.ToString(),
            video.StorageKey,
            expiresUnixTime.ToString(CultureInfo.InvariantCulture));
        var signature = Sign(signaturePayload);
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var escapedVideoId = Uri.EscapeDataString(video.Id.ToString());
        var url = $"{baseUrl}/videos/{escapedVideoId}/playback"
            + $"?expires={expiresUnixTime.ToString(CultureInfo.InvariantCulture)}"
            + $"&signature={Uri.EscapeDataString(signature)}";

        return Task.FromResult(new VideoPlaybackUrl(url, expiresAt));
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

    private void ValidateAllowedStorageProvider(Video video)
    {
        if (!_options.AllowedStorageProviders.Any(provider =>
            string.Equals(provider.Trim(), video.StorageProvider.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Video storage provider is not allowed for playback.");
        }
    }

    private string Sign(string payload)
    {
        var secretBytes = Encoding.UTF8.GetBytes(_options.SigningSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signatureBytes = HMACSHA256.HashData(secretBytes, payloadBytes);

        return Convert.ToHexString(signatureBytes).ToLowerInvariant();
    }
}
