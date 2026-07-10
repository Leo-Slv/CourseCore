using CourseCore.Api.Modules.Courses.Domain.Entities;
using CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Models;

namespace CourseCore.Api.Modules.Courses.Infrastructure.Persistence.Mappers;

public static class CourseModuleMapper
{
    public static CourseModule ToDomain(CourseModulePersistenceModel model)
    {
        var lessons = model.Lessons.Select(LessonMapper.ToDomain);

        return CourseModule.Restore(
            model.Id,
            model.CourseId,
            model.Title,
            model.Description,
            model.DisplayOrder,
            model.Published,
            lessons,
            model.CreatedAt,
            model.UpdatedAt);
    }

    public static CourseModulePersistenceModel ToPersistence(CourseModule module)
    {
        return new CourseModulePersistenceModel
        {
            Id = module.Id,
            CourseId = module.CourseId,
            Title = module.Title,
            Description = module.Description,
            DisplayOrder = module.DisplayOrder,
            Published = module.Published,
            CreatedAt = module.CreatedAt,
            UpdatedAt = module.UpdatedAt,
            Lessons = module.Lessons.Select(LessonMapper.ToPersistence).ToList()
        };
    }

    public static void ApplyChanges(CourseModule module, CourseModulePersistenceModel model)
    {
        model.CourseId = module.CourseId;
        model.Title = module.Title;
        model.Description = module.Description;
        model.DisplayOrder = module.DisplayOrder;
        model.Published = module.Published;
        model.UpdatedAt = module.UpdatedAt;

        model.Lessons.Clear();

        foreach (var lesson in module.Lessons)
        {
            model.Lessons.Add(LessonMapper.ToPersistence(lesson));
        }
    }
}
