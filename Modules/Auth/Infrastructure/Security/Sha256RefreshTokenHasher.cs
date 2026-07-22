using System.Security.Cryptography;
using System.Text;
using CourseCore.Api.Modules.Auth.Application.Contracts;

namespace CourseCore.Api.Modules.Auth.Infrastructure.Security;

public sealed class Sha256RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException("Refresh token is required.", nameof(refreshToken));
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));

        return Convert.ToHexString(bytes);
    }
}
