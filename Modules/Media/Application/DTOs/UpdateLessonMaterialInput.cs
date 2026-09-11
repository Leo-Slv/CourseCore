namespace CourseCore.Api.Modules.Media.Application.DTOs;

public class UpdateLessonMaterialInput
{
    public Guid MaterialId { get; init; }

    public string Title { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }
}
