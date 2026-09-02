using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Domain.Enums;
using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;
using CourseCore.Api.Shared.Domain.ValueObjects;

namespace CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Mappers;

public static class CourseMapper
{
    public static Course ToDomain(CoursePersistenceModel model)
    {
        var modules = model.Modules.Select(CourseModuleMapper.ToDomain);
        var areaIds = model.CourseAreas.Select(x => x.AreaId).Distinct();

        return Course.Restore(
            model.Id,
            model.Title,
            Slug.Create(model.Slug),
            model.Description,
            model.ThumbnailUrl,
            model.Published,
            model.DisplayOrder,
            model.PublishedAt,
            ParsePricingModel(model.PricingModel),
            modules,
            areaIds,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static CoursePersistenceModel ToPersistence(Course course)
    {
        return new CoursePersistenceModel
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
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt,
            CourseAreas = ToCourseAreas(course).ToList(),
            Modules = course.Modules.Select(CourseModuleMapper.ToPersistence).ToList()
        };
    }

    public static void ApplyChanges(Course course, CoursePersistenceModel model)
    {
        model.Title = course.Title;
        model.Slug = course.Slug.Value;
        model.Description = course.Description;
        model.ThumbnailUrl = course.ThumbnailUrl;
        model.Published = course.Published;
        model.DisplayOrder = course.DisplayOrder;
        model.PublishedAt = course.PublishedAt;
        model.PricingModel = course.PricingModel.ToString();
        model.UpdatedAt = course.UpdatedAt;

        model.CourseAreas.Clear();

        foreach (var courseArea in ToCourseAreas(course))
        {
            model.CourseAreas.Add(courseArea);
        }

        // Course update/publish flows do not edit module structure.
        // Preserve existing required child rows to avoid severing EF relationships.
    }

    private static CoursePricingModel ParsePricingModel(string value)
    {
        if (Enum.TryParse<CoursePricingModel>(value, ignoreCase: true, out var pricingModel))
        {
            return pricingModel;
        }

        throw new InvalidOperationException($"Unknown course pricing model '{value}'.");
    }

    private static IEnumerable<CourseAreaPersistenceModel> ToCourseAreas(Course course)
    {
        return course.AreaIds
            .Distinct()
            .Select(areaId => new CourseAreaPersistenceModel
            {
                CourseId = course.Id,
                AreaId = areaId,
                CreatedAt = course.CreatedAt
            });
    }
}
