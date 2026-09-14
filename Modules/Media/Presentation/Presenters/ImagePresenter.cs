using CourseCore.Api.Modules.Media.Application.DTOs;
using CourseCore.Api.Modules.Media.Presentation.Responses;

namespace CourseCore.Api.Modules.Media.Presentation.Presenters;

public static class ImagePresenter
{
    public static ImageUploadUrlResponse ToResponse(ImageUploadUrlOutput output)
    {
        return new ImageUploadUrlResponse
        {
            StorageProvider = output.StorageProvider,
            StorageKey = output.StorageKey,
            UploadUrl = output.UploadUrl,
            PublicUrl = output.PublicUrl,
            ExpiresAt = output.ExpiresAt
        };
    }
}
