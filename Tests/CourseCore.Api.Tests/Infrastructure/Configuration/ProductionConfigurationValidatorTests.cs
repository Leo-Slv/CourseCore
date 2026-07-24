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
    public void ValidateProductionConfiguration_WhenSecretIsTooShort_ShouldThrow()
    {
        var values = ValidValues();
        values["Jwt:SecretKey"] = "short";
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
