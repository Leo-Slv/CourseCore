using System.Security.Cryptography;
using CourseCore.Api.Modules.Auth.Application.Contracts;

namespace CourseCore.Api.Modules.Auth.Infrastructure.Security;

public sealed class SecureEmailVerificationTokenGenerator : IEmailVerificationTokenGenerator
{
    private const int TokenByteLength = 32;

    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
