namespace CourseCore.Api.Modules.Media.Presentation.Requests;

public class ReorderLessonMaterialsRequest
{
    public IReadOnlyCollection<ReorderLessonMaterialItemRequest> Items { get; init; } = [];
}

public class ReorderLessonMaterialItemRequest
{
    public Guid MaterialId { get; init; }

    public int DisplayOrder { get; init; }
}
