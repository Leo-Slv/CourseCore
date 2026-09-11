using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Presentation.Requests;
using CourseCore.Api.Modules.Media.Presentation.Responses;

namespace CourseCore.Api.Modules.Media.Presentation.Presenters;

public static class LessonMaterialPresenter
{
    public static CreateLessonMaterialInput ToInput(Guid lessonId, CreateLessonMaterialRequest request)
    {
        return new CreateLessonMaterialInput
        {
            LessonId = lessonId,
            Title = request.Title,
            FileName = request.FileName,
            ContentType = request.ContentType,
            StorageProvider = request.StorageProvider,
            StorageKey = request.StorageKey,
            SizeBytes = request.SizeBytes,
            DisplayOrder = request.DisplayOrder
        };
    }

    public static UpdateLessonMaterialInput ToInput(Guid materialId, UpdateLessonMaterialRequest request)
    {
        return new UpdateLessonMaterialInput
        {
            MaterialId = materialId,
            Title = request.Title,
            DisplayOrder = request.DisplayOrder
        };
    }

    public static ReorderLessonMaterialsInput ToInput(Guid lessonId, ReorderLessonMaterialsRequest request)
    {
        return new ReorderLessonMaterialsInput
        {
            LessonId = lessonId,
            Items = request.Items
                .Select(item => new ReorderLessonMaterialItem
                {
                    MaterialId = item.MaterialId,
                    DisplayOrder = item.DisplayOrder
                })
                .ToList()
        };
    }

    public static LessonMaterialResponse ToResponse(LessonMaterialOutput output)
    {
        return new LessonMaterialResponse
        {
            Id = output.Id,
            LessonId = output.LessonId,
            Title = output.Title,
            FileName = output.FileName,
            ContentType = output.ContentType,
            StorageProvider = output.StorageProvider,
            StorageKey = output.StorageKey,
            SizeBytes = output.SizeBytes,
            DisplayOrder = output.DisplayOrder,
            CreatedAt = output.CreatedAt,
            UpdatedAt = output.UpdatedAt
        };
    }

    public static IReadOnlyCollection<LessonMaterialResponse> ToResponse(IReadOnlyCollection<LessonMaterialOutput> outputs)
    {
        return outputs.Select(ToResponse).ToList();
    }
}
