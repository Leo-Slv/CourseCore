using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;

namespace CourseCore.Api.Modules.Testimonials.Domain.Entities;

public class Testimonial : EntityBase
{
    private Testimonial(
        string authorName,
        string quote,
        string? avatarUrl,
        Guid? courseId,
        bool published)
    {
        AuthorName = ValidateRequired(authorName, nameof(AuthorName));
        Quote = ValidateRequired(quote, nameof(Quote));
        AvatarUrl = NormalizeOptional(avatarUrl);
        CourseId = courseId == Guid.Empty ? null : courseId;
        Published = published;
    }

    public string AuthorName { get; private set; }

    public string Quote { get; private set; }

    public string? AvatarUrl { get; private set; }

    public Guid? CourseId { get; private set; }

    public bool Published { get; private set; }

    public static Testimonial Create(
        string authorName,
        string quote,
        string? avatarUrl,
        Guid? courseId)
    {
        return new Testimonial(authorName, quote, avatarUrl, courseId, published: false);
    }

    public static Testimonial Restore(
        Guid id,
        string authorName,
        string quote,
        string? avatarUrl,
        Guid? courseId,
        bool published,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new Testimonial(authorName, quote, avatarUrl, courseId, published)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public void ChangeContent(string authorName, string quote, string? avatarUrl, Guid? courseId)
    {
        AuthorName = ValidateRequired(authorName, nameof(AuthorName));
        Quote = ValidateRequired(quote, nameof(Quote));
        AvatarUrl = NormalizeOptional(avatarUrl);
        CourseId = courseId == Guid.Empty ? null : courseId;
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

    private static string ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
