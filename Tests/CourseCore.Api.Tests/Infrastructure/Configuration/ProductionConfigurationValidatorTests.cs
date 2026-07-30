using CourseCore.Api.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace CourseCore.Api.Tests.Infrastructure.Configuration;

public class ProductionConfigurationValidatorTests
{
    [Fact]
    public void ValidateProductionConfiguration_WhenConfigurationIsValid_ShouldNotThrow()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:CourseCoreDatabase"] = "Host=db;Port=5432;Database=coursecore;Username=coursecore;Password=secret",
            ["Jwt:Issuer"] = "CourseCore",
            ["Jwt:Audience"] = "CourseCore",
            ["Jwt:SecretKey"] = "production-secret-with-at-least-32-characters",
            ["Media:Playback:SigningSecret"] = "production-media-secret-with-at-least-32-characters",
            ["Media:Playback:BaseUrl"] = "https://media.coursecore.local",
            ["Media:Playback:SignedUrlExpirationMinutes"] = "10",
            ["Media:Playback:AllowedStorageProviders:0"] = "Local",
            ["Cors:AllowedOrigins:0"] = "https://coursecore.local"
        });

        var exception = Record.Exception(configuration.ValidateProductionConfiguration);

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("ConnectionStrings:CourseCoreDatabase")]
    [InlineData("Jwt:Issuer")]
    [InlineData("Jwt:Audience")]
    [InlineData("Jwt:SecretKey")]
    [InlineData("Media:Playback:SigningSecret")]
    [InlineData("Media:Playback:BaseUrl")]
    public void ValidateProductionConfiguration_WhenRequiredValueIsMissing_ShouldThrow(string key)
    {
        var values = ValidValues();
        values[key] = string.Empty;
        var configuration = CreateConfiguration(values);

        Assert.Throws<InvalidOperationException>(configuration.ValidateProductionConfiguration);
    }

    [Theory]
    [InlineData("CHANGE_ME")]
    [InlineData("SET_BY_ENVIRONMENT")]
    [InlineData("CHANGE_ME_USE_A_LONG_RANDOM_SECRET")]
    public void ValidateProductionConfiguration_WhenSecretUsesPlaceholder_ShouldThrow(string secret)
    {
        var values = ValidValues();
        values["Jwt:SecretKey"] = secret;
        var configuration = CreateConfiguration(values);

        Assert.Throws<InvalidOperationException>(configuration.ValidateProductionConfiguration);
    }

    [Fact]
    public void ValidateProductionConfiguration_WhenMediaSigningSecretUsesPlaceholder_ShouldThrow()
    {
        var values = ValidValues();
        values["Media:Playback:SigningSecret"] = "CHANGE_ME_USE_A_SEPARATE_MEDIA_SIGNING_SECRET";
        var configuration = CreateConfiguration(values);

        Assert.Throws<InvalidOperationException>(configuration.ValidateProductionConfiguration);
    }

    [Fact]
    public void ValidateProductionConfiguration_WhenSecretIsTooShort_ShouldThrow()
    {
        var values = ValidValues();
        values["Jwt:SecretKey"] = "short";
        var configuration = CreateConfiguration(values);

        Assert.Throws<InvalidOperationException>(configuration.ValidateProductionConfiguration);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("61")]
    public void ValidateProductionConfiguration_WhenMediaExpirationIsOutsideRange_ShouldThrow(string expiration)
    {
        var values = ValidValues();
        values["Media:Playback:SignedUrlExpirationMinutes"] = expiration;
        var configuration = CreateConfiguration(values);

        Assert.Throws<InvalidOperationException>(configuration.ValidateProductionConfiguration);
    }

    [Fact]
    public void ValidateProductionConfiguration_WhenCorsOriginsAreMissing_ShouldThrow()
    {
        var values = ValidValues();
        values.Remove("Cors:AllowedOrigins:0");
        var configuration = CreateConfiguration(values);

        Assert.Throws<InvalidOperationException>(configuration.ValidateProductionConfiguration);
    }

    private static Dictionary<string, string?> ValidValues()
    {
        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:CourseCoreDatabase"] = "Host=db;Port=5432;Database=coursecore;Username=coursecore;Password=secret",
            ["Jwt:Issuer"] = "CourseCore",
            ["Jwt:Audience"] = "CourseCore",
            ["Jwt:SecretKey"] = "production-secret-with-at-least-32-characters",
            ["Media:Playback:SigningSecret"] = "production-media-secret-with-at-least-32-characters",
            ["Media:Playback:BaseUrl"] = "https://media.coursecore.local",
            ["Media:Playback:SignedUrlExpirationMinutes"] = "10",
            ["Media:Playback:AllowedStorageProviders:0"] = "Local",
            ["Cors:AllowedOrigins:0"] = "https://coursecore.local"
        };
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
