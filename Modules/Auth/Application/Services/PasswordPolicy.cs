using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Shared.Application.Exceptions;
using System.Text;

namespace CourseCore.Api.Modules.Auth.Application.Services;

public sealed class PasswordPolicy : IPasswordPolicy
{
    public const int MinimumLength = 12;
    public const int MaximumUtf8Bytes = 72;

    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "password123",
        "123456",
        "123456789",
        "qwerty",
        "admin",
        "admin123",
        "coursecore"
    };

    public void Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)
            || password.Length < MinimumLength
            || Encoding.UTF8.GetByteCount(password) > MaximumUtf8Bytes
            || CommonPasswords.Contains(password.Trim()))
        {
            throw new ApplicationValidationException("Password does not meet the security requirements.");
        }
    }
}
