using CourseCore.Api.Modules.Auth.Application.Services;
using CourseCore.Api.Shared.Application.Exceptions;

namespace CourseCore.Api.Tests.Application.Auth;

public class PasswordPolicyTests
{
    private readonly PasswordPolicy _policy = new();

    [Theory]
    [InlineData("short")]
    [InlineData("")]
    [InlineData("            ")]
    [InlineData("password123")]
    [InlineData("coursecore")]
    public void Validate_WhenPasswordIsWeak_ShouldThrowSafeValidationError(string password)
    {
        var exception = Assert.Throws<ApplicationValidationException>(() => _policy.Validate(password));

        Assert.Equal("Password does not meet the security requirements.", exception.Message);
        if (!string.IsNullOrWhiteSpace(password))
        {
            Assert.DoesNotContain(password, exception.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Validate_WhenPasswordIsValid_ShouldNotThrow()
    {
        var exception = Record.Exception(() => _policy.Validate("StrongTestPassword123!"));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WhenPasswordExceedsMaximumLength_ShouldThrow()
    {
        Assert.Throws<ApplicationValidationException>(() =>
            _policy.Validate(new string('A', PasswordPolicy.MaximumUtf8Bytes + 1)));
    }
}
