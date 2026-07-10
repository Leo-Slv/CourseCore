using CourseCore.Api.Modules.Courses.Domain.Entities;
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
        model.UpdatedAt = course.UpdatedAt;

        model.CourseAreas.Clear();

        foreach (var courseArea in ToCourseAreas(course))
        {
            model.CourseAreas.Add(courseArea);
        }

        model.Modules.Clear();

        foreach (var module in course.Modules)
        {
            model.Modules.Add(CourseModuleMapper.ToPersistence(module));
        }
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
