using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Courses.Domain.Entities;

public class Lesson : EntityBase
{
    private Lesson(Guid moduleId, string title, string description, int displayOrder, bool freePreview, bool published)
    {
        ModuleId = ValidateId(moduleId, nameof(ModuleId));
        Title = ValidateRequired(title, nameof(Title));
        Description = NormalizeDescription(description);
        DisplayOrder = ValidateDisplayOrder(displayOrder);
        FreePreview = freePreview;
        Published = published;
    }

    public Guid ModuleId { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool FreePreview { get; private set; }

    public bool Published { get; private set; }

    public static Lesson Create(Guid moduleId, string title, string description, int displayOrder)
    {
        return new Lesson(moduleId, title, description, displayOrder, freePreview: false, published: false);
    }

    public static Lesson Restore(
        Guid id,
        Guid moduleId,
        string title,
        string description,
        int displayOrder,
        bool freePreview,
        bool published,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new Lesson(moduleId, title, description, displayOrder, freePreview, published)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void ChangeTitle(string title)
    {
        Title = ValidateRequired(title, nameof(Title));
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

    public void MarkAsFreePreview()
    {
        FreePreview = true;
        MarkAsUpdated();
    }

    public void RemoveFreePreview()
    {
        FreePreview = false;
        MarkAsUpdated();
    }

    public void Publish()
    {
        Published = true;
        MarkAsUpdated();
    }

    public void Unpublish()
    {
        Published = false;
        MarkAsUpdated();
    }

    private static Guid ValidateId(Guid id, string fieldName)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return id;
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
