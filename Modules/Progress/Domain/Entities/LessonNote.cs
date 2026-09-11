using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Progress.Domain.Entities;

public class LessonNote : EntityBase
{
    private const int MaxContentLength = 10_000;

    private LessonNote(Guid userId, Guid lessonId, string content)
    {
        UserId = ValidateId(userId, nameof(UserId));
        LessonId = ValidateId(lessonId, nameof(LessonId));
        Content = ValidateContent(content);
    }

    public Guid UserId { get; private set; }

    public Guid LessonId { get; private set; }

    public string Content { get; private set; }

    public static LessonNote Create(Guid userId, Guid lessonId, string content)
    {
        return new LessonNote(userId, lessonId, content);
    }

    public static LessonNote Restore(
        Guid id,
        Guid userId,
        Guid lessonId,
        string content,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new LessonNote(userId, lessonId, content)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void ChangeContent(string content)
    {
        Content = ValidateContent(content);
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

    private static string ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("Content is required.");
        }

        var normalized = content.Trim();

        if (normalized.Length > MaxContentLength)
        {
            throw new DomainException("Content is too long.");
        }

        return normalized;
    }
}
