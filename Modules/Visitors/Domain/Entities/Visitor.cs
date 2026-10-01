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
        DateTime updatedAt)
    {
        return new Visitor(name, phone, email, address)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
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
