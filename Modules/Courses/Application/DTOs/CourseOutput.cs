using CourseCore.Api.Modules.Courses.Domain.Entities;

namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class CourseOutput
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? ThumbnailUrl { get; init; }

    public bool Published { get; init; }

    public int DisplayOrder { get; init; }

    public DateTime? PublishedAt { get; init; }

    public string PricingModel { get; init; } = string.Empty;

    public decimal? PriceAmount { get; init; }

    public IReadOnlyCollection<Guid> AreaIds { get; init; } = Array.Empty<Guid>();

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public static CourseOutput FromCourse(Course course)
    {
        return new CourseOutput
        {
            Id = course.Id,
            Title = course.Title,
            Slug = course.Slug.Value,
            Description = course.Description,
            ThumbnailUrl = course.ThumbnailUrl,
            Published = course.Published,
            DisplayOrder = course.DisplayOrder,
            PublishedAt = course.PublishedAt,
            PricingModel = course.PricingModel.ToString(),
            PriceAmount = course.PriceAmount,
            AreaIds = course.AreaIds.ToList(),
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt
        };
    }
}
