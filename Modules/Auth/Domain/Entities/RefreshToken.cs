using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Auth.Domain.Entities;

public class RefreshToken
{
    private RefreshToken(
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime createdAt,
        DateTime? revokedAt,
        string? replacedByTokenHash)
    {
        UserId = userId == Guid.Empty
            ? throw new DomainException("UserId is required.")
            : userId;
        TokenHash = ValidateTokenHash(tokenHash);
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        RevokedAt = revokedAt;
        ReplacedByTokenHash = NormalizeOptionalHash(replacedByTokenHash);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsActive => !IsExpired && !IsRevoked;

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime? createdAt = null)
    {
        return new RefreshToken(
            userId,
            tokenHash,
            expiresAt,
            createdAt ?? DateTime.UtcNow,
            revokedAt: null,
            replacedByTokenHash: null);
    }

    public static RefreshToken Restore(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime createdAt,
        DateTime? revokedAt,
        string? replacedByTokenHash)
    {
        return new RefreshToken(
            userId,
            tokenHash,
            expiresAt,
            createdAt,
            revokedAt,
            replacedByTokenHash)
        {
            Id = id
        };
    }

    public void Revoke(string? replacedByTokenHash = null, DateTime? revokedAt = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = revokedAt ?? DateTime.UtcNow;
        ReplacedByTokenHash = NormalizeOptionalHash(replacedByTokenHash);
    }

    private static string ValidateTokenHash(string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("TokenHash is required.");
        }

        return tokenHash.Trim();
    }

    private static string? NormalizeOptionalHash(string? tokenHash)
    {
        return string.IsNullOrWhiteSpace(tokenHash) ? null : tokenHash.Trim();
    }
}
