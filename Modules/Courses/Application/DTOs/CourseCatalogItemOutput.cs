using CourseCore.Api.Modules.Access.Application.DTOs;
using CourseCore.Api.Modules.Courses.Domain.Entities;

namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class CourseCatalogItemOutput
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? ThumbnailUrl { get; init; }

    public int DisplayOrder { get; init; }

    public string PricingModel { get; init; } = string.Empty;

    public IReadOnlyCollection<Guid> AreaIds { get; init; } = Array.Empty<Guid>();

    public bool HasAccess { get; init; }

    public static CourseCatalogItemOutput FromCatalogEntry(CourseCatalogEntry entry)
    {
        var course = entry.Course;

        return new CourseCatalogItemOutput
        {
            Id = course.Id,
            Title = course.Title,
            Slug = course.Slug.Value,
            Description = course.Description,
            ThumbnailUrl = course.ThumbnailUrl,
            DisplayOrder = course.DisplayOrder,
            PricingModel = course.PricingModel.ToString(),
            AreaIds = course.AreaIds.ToList(),
            HasAccess = entry.HasAccess
        };
    }
}
