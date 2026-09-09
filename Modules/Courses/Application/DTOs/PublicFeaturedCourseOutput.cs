using CourseCore.Api.Modules.Courses.Domain.Entities;

namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class PublicFeaturedCourseOutput
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? ThumbnailUrl { get; init; }

    public string PricingModel { get; init; } = string.Empty;

    public decimal? PriceAmount { get; init; }

    public int ModuleCount { get; init; }

    public int LessonCount { get; init; }

    public int DurationSeconds { get; init; }

    public string? AreaName { get; init; }

    public static PublicFeaturedCourseOutput FromCourse(
        Course course,
        int moduleCount,
        int lessonCount,
        int durationSeconds,
        string? areaName)
    {
        return new PublicFeaturedCourseOutput
        {
            Id = course.Id,
            Title = course.Title,
            Slug = course.Slug.Value,
            Description = course.Description,
            ThumbnailUrl = course.ThumbnailUrl,
            PricingModel = course.PricingModel.ToString(),
            PriceAmount = course.PriceAmount,
            ModuleCount = moduleCount,
            LessonCount = lessonCount,
            DurationSeconds = durationSeconds,
            AreaName = areaName
        };
    }
}
