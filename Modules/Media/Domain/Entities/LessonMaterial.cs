using CourseCore.Api.Modules.Media.Domain.Enums;
using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace CourseCore.Api.Modules.Media.Domain.Entities;

public class LessonMaterial : EntityBase
{
    private const int MaxStorageKeyLength = 1000;
    private static readonly Regex SafeStorageKeyPattern = new("^[A-Za-z0-9][A-Za-z0-9._/-]*$", RegexOptions.Compiled);

    private LessonMaterial(
        Guid lessonId,
        string title,
        string fileName,
        string contentType,
        MaterialStorageProvider storageProvider,
        string storageKey,
        long sizeBytes,
        int displayOrder)
    {
        LessonId = ValidateId(lessonId, nameof(LessonId));
        Title = ValidateRequired(title, nameof(Title));
        FileName = ValidateRequired(fileName, nameof(FileName));
        ContentType = ValidateRequired(contentType, nameof(ContentType));
        StorageProvider = storageProvider;
        StorageKey = ValidateStorageKey(storageKey);
        SizeBytes = ValidateNonNegative(sizeBytes, nameof(SizeBytes));
        DisplayOrder = ValidateNonNegative(displayOrder, nameof(DisplayOrder));
    }

    public Guid LessonId { get; private set; }

    public string Title { get; private set; }

    public string FileName { get; private set; }

    public string ContentType { get; private set; }

    public MaterialStorageProvider StorageProvider { get; private set; }

    public string StorageKey { get; private set; }

    public long SizeBytes { get; private set; }

    public int DisplayOrder { get; private set; }

    public static LessonMaterial Create(
        Guid lessonId,
        string title,
        string fileName,
        string contentType,
        MaterialStorageProvider storageProvider,
        string storageKey,
        long sizeBytes,
        int displayOrder)
    {
        return new LessonMaterial(
            lessonId,
            title,
            fileName,
            contentType,
            storageProvider,
            storageKey,
            sizeBytes,
            displayOrder);
    }

    public static LessonMaterial Restore(
        Guid id,
        Guid lessonId,
        string title,
        string fileName,
        string contentType,
        MaterialStorageProvider storageProvider,
        string storageKey,
        long sizeBytes,
        int displayOrder,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new LessonMaterial(
            lessonId,
            title,
            fileName,
            contentType,
            storageProvider,
            storageKey,
            sizeBytes,
            displayOrder)
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

    public void ChangeDisplayOrder(int displayOrder)
    {
        DisplayOrder = ValidateNonNegative(displayOrder, nameof(DisplayOrder));
        MarkAsUpdated();
    }

    public void ChangeStorage(
        MaterialStorageProvider provider,
        string storageKey,
        string fileName,
        string contentType,
        long sizeBytes)
    {
        StorageProvider = provider;
        StorageKey = ValidateStorageKey(storageKey);
        FileName = ValidateRequired(fileName, nameof(FileName));
        ContentType = ValidateRequired(contentType, nameof(ContentType));
        SizeBytes = ValidateNonNegative(sizeBytes, nameof(SizeBytes));
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

    private static string ValidateStorageKey(string storageKey)
    {
        var normalized = ValidateRequired(storageKey, nameof(StorageKey));

        if (normalized.Length > MaxStorageKeyLength)
        {
            throw new DomainException("StorageKey is too long.");
        }

        if (Uri.TryCreate(normalized, UriKind.Absolute, out _))
        {
            throw new DomainException("StorageKey cannot be a URL.");
        }

        if (normalized.Contains("\\", StringComparison.Ordinal)
            || normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.Contains("//", StringComparison.Ordinal)
            || normalized.Split('/').Any(segment => segment is "." or "..")
            || !SafeStorageKeyPattern.IsMatch(normalized))
        {
            throw new DomainException("StorageKey contains invalid characters.");
        }

        return normalized;
    }

    private static int ValidateNonNegative(int value, string fieldName)
    {
        if (value < 0)
        {
            throw new DomainException($"{fieldName} cannot be negative.");
        }

        return value;
    }

    private static long ValidateNonNegative(long value, string fieldName)
    {
        if (value < 0)
        {
            throw new DomainException($"{fieldName} cannot be negative.");
        }

        return value;
    }
}
