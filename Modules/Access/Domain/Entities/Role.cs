using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Access.Domain.Entities;

public class Role : EntityBase
{
    private Role(string name, string description, bool active)
    {
        Name = ValidateRequired(name, nameof(Name));
        Description = NormalizeDescription(description);
        Active = active;
    }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public bool Active { get; private set; }

    public static Role Create(string name, string description)
    {
        return new Role(name, description, active: true);
    }

    public static Role Restore(Guid id, string name, string description, bool active, DateTime createdAt, DateTime updatedAt)
    {
        return new Role(name, description, active)
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

    private static string NormalizeDescription(string description)
    {
        return description?.Trim() ?? string.Empty;
    }
}
