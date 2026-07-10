using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Access.Domain.Entities;

public class Permission : EntityBase
{
    private Permission(string key, string name, string description)
    {
        Key = ValidateRequired(key, nameof(Key));
        Name = ValidateRequired(name, nameof(Name));
        Description = NormalizeDescription(description);
    }

    public string Key { get; private set; }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public static Permission Create(string key, string name, string description)
    {
        return new Permission(key, name, description);
    }

    public static Permission Restore(Guid id, string key, string name, string description, DateTime createdAt, DateTime updatedAt)
    {
        return new Permission(key, name, description)
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

    public void ChangeDescription(string description)
    {
        Description = NormalizeDescription(description);
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

    private static string NormalizeDescription(string description)
    {
        return description?.Trim() ?? string.Empty;
    }
}
