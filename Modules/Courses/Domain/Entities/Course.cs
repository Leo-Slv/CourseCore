using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Shared.Domain.Entities;
using CourseCore.Api.Shared.Domain.Exceptions;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Courses.Domain.Entities;

public class Course : EntityBase
{
    private readonly List<CourseModule> _modules = [];
    private readonly List<Guid> _areaIds = [];

    private Course(
        string title,
        Slug slug,
        string description,
        string? thumbnailUrl,
        bool published,
        int displayOrder,
        DateTime? publishedAt,
        CoursePricingModel pricingModel,
        decimal? priceAmount,
        bool issuesCertificate,
        bool isFeatured)
    {
        Title = ValidateRequired(title, nameof(Title));
        Slug = slug ?? throw new DomainException("Slug is required.");
        Description = NormalizeDescription(description);
        ThumbnailUrl = NormalizeOptional(thumbnailUrl);
        Published = published;
        DisplayOrder = ValidateDisplayOrder(displayOrder);
        PublishedAt = publishedAt;
        PricingModel = pricingModel;
        PriceAmount = ValidatePriceAmount(pricingModel, priceAmount);
        IssuesCertificate = issuesCertificate;
        IsFeatured = isFeatured;
    }

    public string Title { get; private set; }

    public Slug Slug { get; private set; }

    public string Description { get; private set; }

    public string? ThumbnailUrl { get; private set; }

    public bool Published { get; private set; }

    public int DisplayOrder { get; private set; }

    public DateTime? PublishedAt { get; private set; }

    public CoursePricingModel PricingModel { get; private set; }

    public decimal? PriceAmount { get; private set; }

    public bool IssuesCertificate { get; private set; }

    public bool IsFeatured { get; private set; }

    public IReadOnlyCollection<CourseModule> Modules => _modules.AsReadOnly();

    public IReadOnlyCollection<Guid> AreaIds => _areaIds.AsReadOnly();

    public static Course Create(
        string title,
        Slug slug,
        string description,
        int displayOrder,
        string? thumbnailUrl = null,
        CoursePricingModel pricingModel = CoursePricingModel.Paid,
        decimal? priceAmount = null,
        bool issuesCertificate = true,
        bool isFeatured = false)
    {
        return new Course(title, slug, description, thumbnailUrl, published: false, displayOrder, publishedAt: null, pricingModel, priceAmount, issuesCertificate, isFeatured);
    }

    public static Course Restore(
        Guid id,
        string title,
        Slug slug,
        string description,
        string? thumbnailUrl,
        bool published,
        int displayOrder,
        DateTime? publishedAt,
        CoursePricingModel pricingModel,
        decimal? priceAmount,
        bool issuesCertificate,
        bool isFeatured,
        IEnumerable<CourseModule>? modules,
        IEnumerable<Guid>? areaIds,
        DateTime createdAt,
        DateTime updatedAt)
    {
        var course = new Course(title, slug, description, thumbnailUrl, published, displayOrder, publishedAt, pricingModel, priceAmount, issuesCertificate, isFeatured)
        {
            Id = id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        if (modules is not null)
        {
            foreach (var module in modules)
            {
                course.AddModuleInternal(module);
            }
        }

        if (areaIds is not null)
        {
            foreach (var areaId in areaIds)
            {
                course.AttachAreaInternal(areaId);
            }
        }

        return course;
    }

    public void ChangeTitle(string title)
    {
        Title = ValidateRequired(title, nameof(Title));
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

    public void ChangeThumbnailUrl(string? thumbnailUrl)
    {
        ThumbnailUrl = NormalizeOptional(thumbnailUrl);
        MarkAsUpdated();
    }

    public void ChangeDisplayOrder(int order)
    {
        DisplayOrder = ValidateDisplayOrder(order);
        MarkAsUpdated();
    }

    public void ChangePricingModel(CoursePricingModel pricingModel)
    {
        PricingModel = pricingModel;
        MarkAsUpdated();
    }

    public void ChangePriceAmount(decimal? priceAmount)
    {
        PriceAmount = ValidatePriceAmount(PricingModel, priceAmount);
        MarkAsUpdated();
    }

    public void ChangeCertificateIssuance(bool issuesCertificate)
    {
        IssuesCertificate = issuesCertificate;
        MarkAsUpdated();
    }

    public void ChangeFeatured(bool isFeatured)
    {
        IsFeatured = isFeatured;
        MarkAsUpdated();
    }

    public void AddModule(CourseModule module)
    {
        AddModuleInternal(module);
        MarkAsUpdated();
    }

    public void RemoveModule(Guid moduleId)
    {
        if (moduleId == Guid.Empty)
        {
            throw new DomainException("ModuleId is required.");
        }

        _modules.RemoveAll(module => module.Id == moduleId);
        MarkAsUpdated();
    }

    public void AttachArea(Guid areaId)
    {
        AttachAreaInternal(areaId);
        MarkAsUpdated();
    }

    public void DetachArea(Guid areaId)
    {
        if (areaId == Guid.Empty)
        {
            throw new DomainException("AreaId is required.");
        }

        _areaIds.Remove(areaId);
        MarkAsUpdated();
    }

    public void Publish()
    {
        Published = true;
        PublishedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Unpublish()
    {
        Published = false;
        PublishedAt = null;
        MarkAsUpdated();
    }

    private void AddModuleInternal(CourseModule module)
    {
        if (module is null)
        {
            throw new DomainException("Module is required.");
        }

        if (_modules.Any(existing => existing.Id == module.Id))
        {
            throw new DomainException("Module is already attached to the course.");
        }

        _modules.Add(module);
    }

    private void AttachAreaInternal(Guid areaId)
    {
        if (areaId == Guid.Empty)
        {
            throw new DomainException("AreaId is required.");
        }

        if (_areaIds.Contains(areaId))
        {
            throw new DomainException("Area is already attached to the course.");
        }

        _areaIds.Add(areaId);
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int ValidateDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException("DisplayOrder cannot be negative.");
        }

        return displayOrder;
    }

    private static decimal? ValidatePriceAmount(CoursePricingModel pricingModel, decimal? priceAmount)
    {
        if (priceAmount is null)
        {
            return null;
        }

        if (priceAmount < 0)
        {
            throw new DomainException("PriceAmount cannot be negative.");
        }

        if (pricingModel is CoursePricingModel.Free or CoursePricingModel.EnrollmentControlled)
        {
            throw new DomainException("PriceAmount must not be set for free or enrollment-controlled courses.");
        }

        return priceAmount;
    }
}
