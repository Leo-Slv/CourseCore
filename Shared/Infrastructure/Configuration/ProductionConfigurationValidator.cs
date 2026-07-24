namespace CourseCore.Api.Shared.Infrastructure.Configuration;

public static class ProductionConfigurationValidator
{
    private static readonly string[] PlaceholderValues =
    [
        "CHANGE_ME",
        "SET_BY_ENVIRONMENT",
        "CHANGE_ME_USE_A_LONG_RANDOM_SECRET"
    ];

    public static void ValidateProductionConfiguration(this IConfiguration configuration)
    {
        ValidateRequired(
            configuration.GetConnectionString("CourseCoreDatabase"),
            "ConnectionStrings:CourseCoreDatabase");
        ValidateRequired(configuration["Jwt:Issuer"], "Jwt:Issuer");
        ValidateRequired(configuration["Jwt:Audience"], "Jwt:Audience");
        ValidateSecret(configuration["Jwt:SecretKey"], "Jwt:SecretKey");

        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (allowedOrigins.Length == 0 || allowedOrigins.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one origin in Production.");
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
            string.Equals(value.Trim(), placeholder, StringComparison.OrdinalIgnoreCase));
    }
}
