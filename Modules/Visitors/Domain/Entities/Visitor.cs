using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Visitors.Domain.Entities;

public class Visitor : EntityBase
{
    public const int NameMinLength = 2;
    public const int PhoneMinDigits = 10;
    public const int PhoneMaxDigits = 11;

    private Visitor(string name, string phone, Email email, string? address)
    {
        Name = ValidateName(name);
        Phone = NormalizePhone(phone);
        Email = email ?? throw new DomainException("Email is required.");
        Address = NormalizeOptional(address);
    }

    public string Name { get; private set; }

    public string Phone { get; private set; }

    public Email Email { get; private set; }

    public string? Address { get; private set; }

    public DateTime? ContactedAt { get; private set; }

    public Guid? ContactedByUserId { get; private set; }

    public bool IsContacted => ContactedAt.HasValue;

    public static Visitor Create(string name, string phone, Email email, string? address)
    {
        return new Visitor(name, phone, email, address);
    }

    public static Visitor Restore(
        Guid id,
        string name,
        string phone,
        Email email,
        string? address,
        DateTime createdAt,
        DateTime updatedAt,
        DateTime? contactedAt = null,
        Guid? contactedByUserId = null)
    {
        return new Visitor(name, phone, email, address)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            ContactedAt = contactedAt,
            ContactedByUserId = contactedByUserId
        };
    }

    /// <summary>
    /// Marks the visitor as contacted. Idempotent: an already contacted visitor keeps the
    /// original date and responsible user. Returns true when the state changed.
    /// </summary>
    public bool MarkContacted(Guid contactedByUserId)
    {
        if (contactedByUserId == Guid.Empty)
        {
            throw new DomainException("ContactedByUserId is required.");
        }

        if (IsContacted)
        {
            return false;
        }

        ContactedAt = DateTime.UtcNow;
        ContactedByUserId = contactedByUserId;
        MarkAsUpdated();

        return true;
    }

    /// <summary>Returns true when the state changed.</summary>
    public bool ClearContacted()
    {
        if (!IsContacted)
        {
            return false;
        }

        ContactedAt = null;
        ContactedByUserId = null;
        MarkAsUpdated();

        return true;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < NameMinLength)
        {
            throw new DomainException("Name is invalid.");
        }

        return name.Trim();
    }

    private static string NormalizePhone(string phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

        if (digits.Length is < PhoneMinDigits or > PhoneMaxDigits)
        {
            throw new DomainException("Phone is invalid.");
        }

        return digits;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
