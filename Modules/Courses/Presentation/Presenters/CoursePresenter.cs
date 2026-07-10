using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Presentation.Requests;
using CourseCore.Api.Modules.Courses.Presentation.Responses;

namespace CourseCore.Api.Modules.Courses.Presentation.Presenters;

public static class CoursePresenter
{
    public static CreateCourseInput ToInput(CreateCourseRequest request)
    {
        return new CreateCourseInput
        {
            Title = request.Title,
            Slug = request.Slug,
            Description = request.Description,
            ThumbnailUrl = request.ThumbnailUrl,
            DisplayOrder = request.DisplayOrder,
            AreaIds = request.AreaIds.ToList(),
            Modules = request.Modules.Select(ToInput).ToList()
        };
    }

    public static CreateCourseModuleInput ToInput(CreateCourseModuleRequest request)
    {
        return new CreateCourseModuleInput
        {
            Title = request.Title,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder,
            Lessons = request.Lessons.Select(ToInput).ToList()
        };
    }

    public static CreateLessonInput ToInput(CreateLessonRequest request)
    {
        return new CreateLessonInput
        {
            Title = request.Title,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder,
            FreePreview = request.FreePreview
        };
    }

    public static UpdateCourseInput ToInput(Guid courseId, UpdateCourseRequest request)
    {
        return new UpdateCourseInput
        {
            CourseId = courseId,
            Title = request.Title,
            Slug = request.Slug,
            Description = request.Description,
            ThumbnailUrl = request.ThumbnailUrl,
            DisplayOrder = request.DisplayOrder,
            AreaIds = request.AreaIds.ToList()
        };
    }

    public static CourseResponse ToResponse(CourseOutput output)
    {
        return new CourseResponse
        {
            Id = output.Id,
            Title = output.Title,
            Slug = output.Slug,
            Description = output.Description,
            ThumbnailUrl = output.ThumbnailUrl,
            Published = output.Published,
            DisplayOrder = output.DisplayOrder,
            PublishedAt = output.PublishedAt,
            AreaIds = output.AreaIds.ToList(),
            CreatedAt = output.CreatedAt,
            UpdatedAt = output.UpdatedAt
        };
    }

    public static CourseDetailsResponse ToResponse(CourseDetailsOutput output)
    {
        return new CourseDetailsResponse
        {
            Id = output.Id,
            Title = output.Title,
            Slug = output.Slug,
            Description = output.Description,
            ThumbnailUrl = output.ThumbnailUrl,
            Published = output.Published,
            DisplayOrder = output.DisplayOrder,
            PublishedAt = output.PublishedAt,
            AreaIds = output.AreaIds.ToList(),
            Modules = output.Modules.Select(ToResponse).ToList(),
            CreatedAt = output.CreatedAt,
            UpdatedAt = output.UpdatedAt
        };
    }

    public static CourseListItemResponse ToResponse(CourseListItemOutput output)
    {
        return new CourseListItemResponse
        {
            Id = output.Id,
            Title = output.Title,
            Slug = output.Slug,
            Description = output.Description,
            ThumbnailUrl = output.ThumbnailUrl,
            DisplayOrder = output.DisplayOrder
        };
    }

    public static CourseModuleResponse ToResponse(CourseModuleOutput output)
    {
        return new CourseModuleResponse
        {
            Id = output.Id,
            CourseId = output.CourseId,
            Title = output.Title,
            Description = output.Description,
            DisplayOrder = output.DisplayOrder,
            Published = output.Published,
            Lessons = output.Lessons.Select(ToResponse).ToList()
        };
    }

    public static LessonResponse ToResponse(LessonOutput output)
    {
        return new LessonResponse
        {
            Id = output.Id,
            ModuleId = output.ModuleId,
            Title = output.Title,
            Description = output.Description,
            DisplayOrder = output.DisplayOrder,
            FreePreview = output.FreePreview,
            Published = output.Published
        };
    }

    public static IReadOnlyCollection<CourseListItemResponse> ToResponse(IReadOnlyCollection<CourseListItemOutput> outputs)
    {
        return outputs.Select(ToResponse).ToList();
    }
}
