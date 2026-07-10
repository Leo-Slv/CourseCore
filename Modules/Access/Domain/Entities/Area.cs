using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Access.Domain.Entities;

public class Area : EntityBase
{
    private Area(string name, Slug slug, string description, bool active, int displayOrder)
    {
        Name = ValidateRequired(name, nameof(Name));
        Slug = slug ?? throw new DomainException("Slug is required.");
        Description = NormalizeDescription(description);
        Active = active;
        DisplayOrder = ValidateDisplayOrder(displayOrder);
    }

    public string Name { get; private set; }

    public Slug Slug { get; private set; }

    public string Description { get; private set; }

    public bool Active { get; private set; }

    public int DisplayOrder { get; private set; }

    public static Area Create(string name, Slug slug, string description, int displayOrder)
    {
        return new Area(name, slug, description, active: true, displayOrder);
    }

    public static Area Restore(
        Guid id,
        string name,
        Slug slug,
        string description,
        bool active,
        int displayOrder,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new Area(name, slug, description, active, displayOrder)
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

    public void ChangeSlug(Slug slug)
    {
        Slug = slug ?? throw new DomainException("Slug is required.");
        MarkAsUpdated();
    }

    public void ChangeDescription(string description)
    {
        Description = NormalizeDescription(description);
        MarkAsUpdated();
    }

    public void ChangeDisplayOrder(int order)
    {
        DisplayOrder = ValidateDisplayOrder(order);
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

    private static int ValidateDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException("DisplayOrder cannot be negative.");
        }

        return displayOrder;
    }
}
