using System.Security.Cryptography;
using System.Text;
using CourseCore.Api.Modules.Auth.Application.Contracts;

namespace CourseCore.Api.Modules.Auth.Infrastructure.Security;

public sealed class Sha256EmailVerificationTokenHasher : IEmailVerificationTokenHasher
{
    public string Hash(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token is required.", nameof(token));
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(bytes);
    }
}
