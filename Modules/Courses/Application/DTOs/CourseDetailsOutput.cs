using CourseCore.Api.Modules.Courses.Domain.Entities;

namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class CourseDetailsOutput
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

    public bool HasAccess { get; init; }

    public IReadOnlyCollection<Guid> AreaIds { get; init; } = Array.Empty<Guid>();

    public IReadOnlyCollection<CourseModuleOutput> Modules { get; init; } = Array.Empty<CourseModuleOutput>();

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public static CourseDetailsOutput FromCourse(
        Course course,
        bool hasAccess,
        IReadOnlyDictionary<Guid, (Guid VideoId, int DurationSeconds)> videoInfoByLessonId)
    {
        return new CourseDetailsOutput
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
            HasAccess = hasAccess,
            AreaIds = course.AreaIds.ToList(),
            Modules = course.Modules
                .OrderBy(module => module.DisplayOrder)
                .Select(module => CourseModuleOutput.FromModule(module, videoInfoByLessonId, hasAccess))
                .ToList(),
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt
        };
    }
}
