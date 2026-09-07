using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Auth.Domain.Entities;

public class PasswordResetToken
{
    private PasswordResetToken(
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime createdAt,
        DateTime? consumedAt)
    {
        UserId = userId == Guid.Empty
            ? throw new DomainException("UserId is required.")
            : userId;
        TokenHash = ValidateTokenHash(tokenHash);
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        ConsumedAt = consumedAt;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ConsumedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsConsumed => ConsumedAt.HasValue;

    public bool IsActive => !IsExpired && !IsConsumed;

    public static PasswordResetToken Create(
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime? createdAt = null)
    {
        return new PasswordResetToken(
            userId,
            tokenHash,
            expiresAt,
            createdAt ?? DateTime.UtcNow,
            consumedAt: null);
    }

    public static PasswordResetToken Restore(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime createdAt,
        DateTime? consumedAt)
    {
        return new PasswordResetToken(userId, tokenHash, expiresAt, createdAt, consumedAt)
        {
            Id = id
        };
    }

    public void Consume(DateTime? consumedAt = null)
    {
        if (IsConsumed)
        {
            return;
        }

        ConsumedAt = consumedAt ?? DateTime.UtcNow;
    }

    private static string ValidateTokenHash(string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("TokenHash is required.");
        }

        return tokenHash.Trim();
    }
}
