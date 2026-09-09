using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Users.Domain.Entities;

public class User : EntityBase
{
    private User(
        string name,
        Email email,
        string passwordHash,
        bool active,
        DateTime? emailVerifiedAt,
        int tokenVersion,
        string? phone,
        string? avatarUrl)
    {
        Name = ValidateRequired(name, nameof(Name));
        Email = email ?? throw new DomainException("Email is required.");
        PasswordHash = ValidateRequired(passwordHash, nameof(PasswordHash));
        Active = active;
        EmailVerifiedAt = emailVerifiedAt;
        TokenVersion = ValidateTokenVersion(tokenVersion);
        Phone = NormalizeOptional(phone);
        AvatarUrl = NormalizeOptional(avatarUrl);
    }

    public string Name { get; private set; }

    public Email Email { get; private set; }

    public string PasswordHash { get; private set; }

    public bool Active { get; private set; }

    public DateTime? EmailVerifiedAt { get; private set; }

    public int TokenVersion { get; private set; }

    public string? Phone { get; private set; }

    public string? AvatarUrl { get; private set; }

    public static User Create(string name, Email email, string passwordHash)
    {
        return new User(
            name, email, passwordHash, active: true, emailVerifiedAt: null, tokenVersion: 0,
            phone: null, avatarUrl: null);
    }

    public static User Restore(
        Guid id,
        string name,
        Email email,
        string passwordHash,
        bool active,
        DateTime? emailVerifiedAt,
        int tokenVersion,
        DateTime createdAt,
        DateTime updatedAt,
        string? phone = null,
        string? avatarUrl = null)
    {
        return new User(name, email, passwordHash, active, emailVerifiedAt, tokenVersion, phone, avatarUrl)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void ChangeName(string name)
    {
        Name = ValidateRequired(name, nameof(Name));
        MarkAsUpdated();
    }

    public void ChangePhone(string? phone)
    {
        Phone = NormalizeOptional(phone);
        MarkAsUpdated();
    }

    public void ChangeAvatarUrl(string? avatarUrl)
    {
        AvatarUrl = NormalizeOptional(avatarUrl);
        MarkAsUpdated();
    }

    public void ChangeEmail(Email email)
    {
        Email = email ?? throw new DomainException("Email is required.");
        MarkAsUpdated();
    }

    public void ChangePasswordHash(string passwordHash)
    {
        PasswordHash = ValidateRequired(passwordHash, nameof(PasswordHash));
        MarkAsUpdated();
    }

    public void IncrementTokenVersion()
    {
        TokenVersion++;
        MarkAsUpdated();
    }

    public void MarkEmailAsVerified(DateTime? verifiedAt = null)
    {
        EmailVerifiedAt = verifiedAt ?? DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Activate()
    {
        Active = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        Active = false;
        MarkAsUpdated();
    }

    private static string ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static int ValidateTokenVersion(int tokenVersion)
    {
        if (tokenVersion < 0)
        {
            throw new DomainException("TokenVersion cannot be negative.");
        }

        return tokenVersion;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
