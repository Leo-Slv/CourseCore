namespace CourseCore.Api.Modules.Media.Application.Options;

public sealed class MediaPlaybackOptions
{
    public const string SectionName = "Media:Playback";

    public int SignedUrlExpirationMinutes { get; init; } = 10;

    public string SigningSecret { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = "/media";

    public IReadOnlyCollection<string> AllowedStorageProviders { get; init; } = ["Local"];

    public static void Validate(MediaPlaybackOptions options, bool requireSigningSecret)
    {
        if (options.SignedUrlExpirationMinutes is < 1 or > 60)
        {
            throw new InvalidOperationException("Media playback signed URL expiration must be between 1 and 60 minutes.");
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new InvalidOperationException("Media playback base URL is required.");
        }

        if (options.AllowedStorageProviders.Count == 0 || options.AllowedStorageProviders.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Media playback allowed storage providers must contain at least one provider.");
        }

        if (requireSigningSecret)
        {
            ValidateSigningSecret(options.SigningSecret);
        }
    }

    public static void ValidateSigningSecret(string? signingSecret)
    {
        if (string.IsNullOrWhiteSpace(signingSecret)
            || signingSecret.Trim().Equals("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
            || signingSecret.Trim().Equals("SET_BY_ENVIRONMENT", StringComparison.OrdinalIgnoreCase)
            || signingSecret.Trim().Equals("CHANGE_ME_USE_A_LONG_RANDOM_SECRET", StringComparison.OrdinalIgnoreCase)
            || signingSecret.Trim().Equals("CHANGE_ME_USE_A_SEPARATE_MEDIA_SIGNING_SECRET", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Media playback signing secret must be configured.");
        }

        if (signingSecret.Length < 32)
        {
            throw new InvalidOperationException("Media playback signing secret must contain at least 32 characters.");
        }
    }
}
