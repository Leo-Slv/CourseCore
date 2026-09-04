using CourseCore.Api.Modules.Courses.Domain.Entities;

namespace CourseCore.Api.Modules.Courses.Application.DTOs;

public class CourseModuleOutput
{
    public Guid Id { get; init; }

    public Guid CourseId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool Published { get; init; }

    public IReadOnlyCollection<LessonOutput> Lessons { get; init; } = Array.Empty<LessonOutput>();

    public static CourseModuleOutput FromModule(
        CourseModule module,
        IReadOnlyDictionary<Guid, (Guid VideoId, int DurationSeconds)> videoInfoByLessonId)
    {
        return new CourseModuleOutput
        {
            Id = module.Id,
            CourseId = module.CourseId,
            Title = module.Title,
            Description = module.Description,
            DisplayOrder = module.DisplayOrder,
            Published = module.Published,
            Lessons = module.Lessons
                .OrderBy(lesson => lesson.DisplayOrder)
                .Select(lesson => videoInfoByLessonId.TryGetValue(lesson.Id, out var videoInfo)
                    ? LessonOutput.FromLesson(lesson, videoInfo.VideoId, videoInfo.DurationSeconds)
                    : LessonOutput.FromLesson(lesson, videoId: null, durationSeconds: null))
                .ToList()
        };
    }
}
