using System.Text.RegularExpressions;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Shared.Domain.ValueObjects;

public sealed partial class Slug : IEquatable<Slug>
{
    private Slug(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Slug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Slug is required.");
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (!SlugPattern().IsMatch(normalized))
        {
            throw new DomainException("Slug is invalid.");
        }

        return new Slug(normalized);
    }

    public override string ToString()
    {
        return Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Slug);
    }

    public bool Equals(Slug? other)
    {
        return other is not null && Value == other.Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode(StringComparison.Ordinal);
    }

    public static bool operator ==(Slug? left, Slug? right)
    {
        return EqualityComparer<Slug>.Default.Equals(left, right);
    }

    public static bool operator !=(Slug? left, Slug? right)
    {
        return !(left == right);
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
