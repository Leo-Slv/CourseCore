using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Courses.Domain.Entities;

public class CourseModule : EntityBase
{
    private readonly List<Lesson> _lessons = [];

    private CourseModule(Guid courseId, string title, string description, int displayOrder, bool published)
    {
        CourseId = ValidateId(courseId, nameof(CourseId));
        Title = ValidateRequired(title, nameof(Title));
        Description = NormalizeDescription(description);
        DisplayOrder = ValidateDisplayOrder(displayOrder);
        Published = published;
    }

    public Guid CourseId { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool Published { get; private set; }

    public IReadOnlyCollection<Lesson> Lessons => _lessons.AsReadOnly();

    public static CourseModule Create(Guid courseId, string title, string description, int displayOrder)
    {
        return new CourseModule(courseId, title, description, displayOrder, published: false);
    }

    public static CourseModule Restore(
        Guid id,
        Guid courseId,
        string title,
        string description,
        int displayOrder,
        bool published,
        IEnumerable<Lesson>? lessons,
        DateTime createdAt,
        DateTime updatedAt)
    {
        var module = new CourseModule(courseId, title, description, displayOrder, published)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        if (lessons is not null)
        {
            foreach (var lesson in lessons)
            {
                module.AddLessonInternal(lesson);
            }
        }

        return module;
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

    public void AddLesson(Lesson lesson)
    {
        AddLessonInternal(lesson);
        MarkAsUpdated();
    }

    public void RemoveLesson(Guid lessonId)
    {
        if (lessonId == Guid.Empty)
        {
            throw new DomainException("LessonId is required.");
        }

        _lessons.RemoveAll(lesson => lesson.Id == lessonId);
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

    private void AddLessonInternal(Lesson lesson)
    {
        if (lesson is null)
        {
            throw new DomainException("Lesson is required.");
        }

        if (_lessons.Any(existing => existing.Id == lesson.Id))
        {
            throw new DomainException("Lesson is already attached to the module.");
        }

        _lessons.Add(lesson);
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
