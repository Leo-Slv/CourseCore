using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CourseCore.Api.Modules.Auth.Application.Contracts;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Users.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CourseCore.Api.Modules.Auth.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public Task<string> GenerateAccessTokenAsync(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ValidateOptions(_options);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email.Value),
            new(ClaimTypes.Name, user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(role => new Claim(AuthClaimTypes.Role, role)));
        claims.AddRange(permissions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(permission => new Claim(AuthClaimTypes.Permission, permission)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_options.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    internal static void ValidateOptions(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("JWT issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("JWT audience is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey) || options.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT secret key must contain at least 32 characters.");
        }

        if (options.AccessTokenExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("JWT access token expiration must be greater than zero.");
        }

        if (options.RefreshTokenExpirationDays <= 0)
        {
            throw new InvalidOperationException("JWT refresh token expiration must be greater than zero.");
        }
    }
}
