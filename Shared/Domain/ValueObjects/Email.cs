using System.Net.Mail;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Shared.Domain.ValueObjects;

public sealed class Email : IEquatable<Email>
{
    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Email is required.");
        }

        var normalized = value.Trim().ToLowerInvariant();

        try
        {
            var address = new MailAddress(normalized);

            if (!string.Equals(address.Address, normalized, StringComparison.Ordinal))
            {
                throw new DomainException("Email is invalid.");
            }
        }
        catch (FormatException ex)
        {
            throw new DomainException("Email is invalid.", ex);
        }

        return new Email(normalized);
    }

    public override string ToString()
    {
        return Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Email);
    }

    public bool Equals(Email? other)
    {
        return other is not null && Value == other.Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode(StringComparison.Ordinal);
    }

    public static bool operator ==(Email? left, Email? right)
    {
        return EqualityComparer<Email>.Default.Equals(left, right);
    }

    public static bool operator !=(Email? left, Email? right)
    {
        return !(left == right);
    }
}
