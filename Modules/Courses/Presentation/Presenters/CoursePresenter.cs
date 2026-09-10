using CourseCore.Api.Modules.Courses.Application.DTOs;
using CourseCore.Api.Modules.Courses.Presentation.Requests;
using CourseCore.Api.Modules.Courses.Presentation.Responses;

namespace CourseCore.Api.Modules.Courses.Presentation.Presenters;

public static class CoursePresenter
{
    public static PublicCatalogSummaryResponse ToResponse(PublicCatalogSummaryOutput output)
    {
        return new PublicCatalogSummaryResponse
        {
            ActiveAreaCount = output.ActiveAreaCount,
            PublishedCourseCount = output.PublishedCourseCount,
            FeaturedCourses = output.FeaturedCourses.Select(ToResponse).ToList(),
            HighlightedCourse = output.HighlightedCourse is null ? null : ToResponse(output.HighlightedCourse),
            Areas = output.Areas.Select(ToResponse).ToList()
        };
    }

    public static PublicFeaturedCourseResponse ToResponse(PublicFeaturedCourseOutput output)
    {
        return new PublicFeaturedCourseResponse
        {
            Id = output.Id,
            Title = output.Title,
            Slug = output.Slug,
            Description = output.Description,
            ThumbnailUrl = output.ThumbnailUrl,
            PricingModel = output.PricingModel,
            PriceAmount = output.PriceAmount,
            ModuleCount = output.ModuleCount,
            LessonCount = output.LessonCount,
            DurationSeconds = output.DurationSeconds,
            AreaName = output.AreaName
        };
    }

    public static PublicAreaSummaryResponse ToResponse(PublicAreaSummaryOutput output)
    {
        return new PublicAreaSummaryResponse
        {
            Id = output.Id,
            Name = output.Name,
            Slug = output.Slug,
            PublishedCourseCount = output.PublishedCourseCount
        };
    }

    public static CreateCourseInput ToInput(CreateCourseRequest request)
    {
        return new CreateCourseInput
        {
            Title = request.Title,
            Slug = request.Slug,
            Description = request.Description,
            ThumbnailUrl = request.ThumbnailUrl,
            DisplayOrder = request.DisplayOrder,
            PricingModel = request.PricingModel,
            PriceAmount = request.PriceAmount,
            IssuesCertificate = request.IssuesCertificate,
            IsFeatured = request.IsFeatured,
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
            PricingModel = request.PricingModel,
            PriceAmount = request.PriceAmount,
            IssuesCertificate = request.IssuesCertificate,
            IsFeatured = request.IsFeatured,
            AreaIds = request.AreaIds.ToList()
        };
    }

    public static AddCourseModuleInput ToInput(Guid courseId, AddCourseModuleRequest request)
    {
        return new AddCourseModuleInput
        {
            CourseId = courseId,
            Title = request.Title,
            Description = request.Description
        };
    }

    public static UpdateCourseModuleInput ToInput(Guid moduleId, UpdateCourseModuleRequest request)
    {
        return new UpdateCourseModuleInput
        {
            ModuleId = moduleId,
            Title = request.Title,
            Description = request.Description,
            Published = request.Published
        };
    }

    public static ReorderCourseModulesInput ToInput(Guid courseId, ReorderCourseModulesRequest request)
    {
        return new ReorderCourseModulesInput
        {
            CourseId = courseId,
            OrderedModuleIds = request.ModuleIds.ToList()
        };
    }

    public static AddLessonInput ToInput(Guid moduleId, AddLessonRequest request)
    {
        return new AddLessonInput
        {
            ModuleId = moduleId,
            Title = request.Title,
            Description = request.Description,
            FreePreview = request.FreePreview
        };
    }

    public static UpdateLessonInput ToInput(Guid lessonId, UpdateLessonRequest request)
    {
        return new UpdateLessonInput
        {
            LessonId = lessonId,
            Title = request.Title,
            Description = request.Description,
            FreePreview = request.FreePreview,
            Published = request.Published
        };
    }

    public static MoveLessonInput ToInput(Guid lessonId, MoveLessonRequest request)
    {
        return new MoveLessonInput
        {
            LessonId = lessonId,
            TargetModuleId = request.TargetModuleId
        };
    }

    public static ReorderLessonsInput ToInput(Guid moduleId, ReorderLessonsRequest request)
    {
        return new ReorderLessonsInput
        {
            ModuleId = moduleId,
            OrderedLessonIds = request.LessonIds.ToList()
        };
    }

    public static GetCourseDetailsInput ToGetCourseDetailsInput(Guid courseId, Guid userId)
    {
        return new GetCourseDetailsInput
        {
            UserId = userId,
            CourseId = courseId
        };
    }

    public static ListAvailableCoursesInput ToInput(Guid userId, ListAvailableCoursesRequest request)
    {
        return new ListAvailableCoursesInput
        {
            UserId = userId,
            HasAccess = request.HasAccess
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
            PricingModel = output.PricingModel,
            PriceAmount = output.PriceAmount,
            IssuesCertificate = output.IssuesCertificate,
            IsFeatured = output.IsFeatured,
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
            PricingModel = output.PricingModel,
            PriceAmount = output.PriceAmount,
            HasAccess = output.HasAccess,
            CertificateIssued = output.CertificateIssued,
            AreaIds = output.AreaIds.ToList(),
            Modules = output.Modules.Select(ToResponse).ToList(),
            CreatedAt = output.CreatedAt,
            UpdatedAt = output.UpdatedAt
        };
    }

    public static AreaSummaryResponse ToResponse(AreaSummaryOutput output)
    {
        return new AreaSummaryResponse
        {
            Id = output.Id,
            Name = output.Name,
            Slug = output.Slug,
            Description = output.Description,
            DisplayOrder = output.DisplayOrder
        };
    }

    public static CourseCatalogItemResponse ToResponse(CourseCatalogItemOutput output)
    {
        return new CourseCatalogItemResponse
        {
            Id = output.Id,
            Title = output.Title,
            Slug = output.Slug,
            Description = output.Description,
            ThumbnailUrl = output.ThumbnailUrl,
            DisplayOrder = output.DisplayOrder,
            PricingModel = output.PricingModel,
            PriceAmount = output.PriceAmount,
            AreaIds = output.AreaIds.ToList(),
            HasAccess = output.HasAccess,
            ModuleCount = output.ModuleCount,
            LessonCount = output.LessonCount,
            DurationSeconds = output.DurationSeconds,
            CertificateIssued = output.CertificateIssued
        };
    }

    public static CourseCatalogResponse ToResponse(CourseCatalogOutput output)
    {
        return new CourseCatalogResponse
        {
            Areas = output.Areas.Select(ToResponse).ToList(),
            Courses = output.Courses.Select(ToResponse).ToList()
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
            Published = output.Published,
            VideoId = output.VideoId,
            DurationSeconds = output.DurationSeconds
        };
    }

}
