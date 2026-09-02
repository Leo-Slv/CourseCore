namespace CourseCore.Api.Shared.Infrastructure.Configuration;

public static class ProductionConfigurationValidator
{
    private static readonly string[] PlaceholderValues =
    [
        "CHANGE_ME",
        "SET_BY_ENVIRONMENT",
        "CHANGE_ME_USE_A_LONG_RANDOM_SECRET",
        "REPLACE_WITH_RANDOM_JWT_SECRET_AT_LEAST_32_CHARACTERS",
        "REPLACE_WITH_DIFFERENT_RANDOM_MEDIA_SECRET_AT_LEAST_32_CHARACTERS"
    ];

    public static void ValidateProductionConfiguration(this IConfiguration configuration)
    {
        ValidateRequired(
            configuration.GetConnectionString("CourseCoreDatabase"),
            "ConnectionStrings:CourseCoreDatabase");
        ValidateRequired(configuration["Jwt:Issuer"], "Jwt:Issuer");
        ValidateRequired(configuration["Jwt:Audience"], "Jwt:Audience");
        ValidateSecret(configuration["Jwt:SecretKey"], "Jwt:SecretKey");
        ValidateSecret(configuration["Media:Playback:SigningSecret"], "Media:Playback:SigningSecret");
        ValidateSecretsAreDistinct(
            configuration["Jwt:SecretKey"]!,
            configuration["Media:Playback:SigningSecret"]!);
        ValidateRefreshTokenCookie(configuration);
        ValidateRequired(configuration["Media:Playback:BaseUrl"], "Media:Playback:BaseUrl");
        ValidatePlaybackExpiration(configuration["Media:Playback:SignedUrlExpirationMinutes"]);

        var allowedStorageProviders = configuration
            .GetSection("Media:Playback:AllowedStorageProviders")
            .Get<string[]>() ?? [];

        if (allowedStorageProviders.Length == 0 || allowedStorageProviders.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Media:Playback:AllowedStorageProviders must contain at least one provider in Production.");
        }

        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (allowedOrigins.Length == 0 || allowedOrigins.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one origin in Production.");
        }

        ValidateRequired(configuration["Turnstile:SecretKey"], "Turnstile:SecretKey");
        ValidateRequired(configuration["Resend:ApiKey"], "Resend:ApiKey");
        ValidateRequired(configuration["Resend:FromAddress"], "Resend:FromAddress");
    }

    private static void ValidateSecretsAreDistinct(string jwtSecret, string mediaSigningSecret)
    {
        if (string.Equals(jwtSecret.Trim(), mediaSigningSecret.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Media:Playback:SigningSecret must be different from Jwt:SecretKey in Production.");
        }
    }

    private static void ValidateRefreshTokenCookie(IConfiguration configuration)
    {
        var secure = configuration.GetValue<bool>("Auth:RefreshTokenCookie:Secure");
        var sameSite = configuration["Auth:RefreshTokenCookie:SameSite"];

        if (string.Equals(sameSite?.Trim(), "None", StringComparison.OrdinalIgnoreCase) && !secure)
        {
            throw new InvalidOperationException("Auth:RefreshTokenCookie SameSite=None requires Secure=true in Production.");
        }

        if (!secure)
        {
            throw new InvalidOperationException("Auth:RefreshTokenCookie:Secure must be true in Production.");
        }
    }

    private static void ValidateRequired(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value) || IsPlaceholder(value))
        {
            throw new InvalidOperationException($"{key} must be configured for Production.");
        }
    }

    private static void ValidateSecret(string? value, string key)
    {
        ValidateRequired(value, key);

        if (value!.Length < 32)
        {
            throw new InvalidOperationException($"{key} must contain at least 32 characters in Production.");
        }
    }

    private static bool IsPlaceholder(string value)
    {
        return PlaceholderValues.Any(placeholder =>
            string.Equals(value.Trim(), placeholder, StringComparison.OrdinalIgnoreCase))
            || string.Equals(
                value.Trim(),
                "CHANGE_ME_USE_A_SEPARATE_MEDIA_SIGNING_SECRET",
                StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidatePlaybackExpiration(string? value)
    {
        if (!int.TryParse(value, out var expirationMinutes) || expirationMinutes is < 1 or > 60)
        {
            throw new InvalidOperationException("Media:Playback:SignedUrlExpirationMinutes must be between 1 and 60 in Production.");
        }
    }
}
